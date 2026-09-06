using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NPoco;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Analytics;

public readonly record struct PageView(DateTime OccurredAt, string Path, string? VisitorHash, bool IsBot);

public interface IPageViewTracker
{
    void Track(PageView view);
}

public sealed class PageViewTracker : IPageViewTracker
{
    private readonly Channel<PageView> _channel = Channel.CreateBounded<PageView>(
        new BoundedChannelOptions(10_000) { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true });

    public ChannelReader<PageView> Reader => _channel.Reader;

    public void Track(PageView view) => _channel.Writer.TryWrite(view);
}

public sealed class PageViewWriter(PageViewTracker tracker, IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var reader = tracker.Reader;
        var batch = new List<PageView>(128);
        while (await reader.WaitToReadAsync(stoppingToken))
        {
            batch.Clear();
            while (batch.Count < 128 && reader.TryRead(out var view)) batch.Add(view);
            if (batch.Count == 0) continue;
            try { await FlushAsync(batch); }
            catch { }
        }
    }

    private async Task FlushAsync(IReadOnlyList<PageView> batch)
    {
        using var diScope = scopeFactory.CreateScope();
        var scopeProvider = diScope.ServiceProvider.GetRequiredService<IScopeProvider>();
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        foreach (var v in batch)
        {
            await scope.Database.ExecuteAsync(
                "INSERT INTO PageViews (OccurredAt, Path, VisitorHash, IsBot) VALUES (@0, @1, @2, @3)",
                v.OccurredAt, v.Path, v.VisitorHash, v.IsBot);
        }
    }
}

public sealed class PageViewTrackerComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<PageViewTracker>();
        builder.Services.AddSingleton<IPageViewTracker>(sp => sp.GetRequiredService<PageViewTracker>());
        builder.Services.AddHostedService<PageViewWriter>();
    }
}
