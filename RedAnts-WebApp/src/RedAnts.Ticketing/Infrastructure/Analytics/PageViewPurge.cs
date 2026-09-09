using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NPoco;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Infrastructure.Analytics;

public sealed class PageViewPurge(IServiceScopeFactory scopes, ILogger<PageViewPurge> logger) : BackgroundService
{
    public static readonly TimeSpan Retention = TimeSpan.FromDays(365);
    public static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    public static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(2);
    private const int BatchSize = 5000;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(StartupDelay, stoppingToken); }
        catch (OperationCanceledException) { return; }

        await PurgeAsync(stoppingToken);
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await PurgeAsync(stoppingToken);
    }

    private async Task PurgeAsync(CancellationToken stoppingToken)
    {
        try
        {
            var cutoff = DateTime.UtcNow - Retention;
            using var diScope = scopes.CreateScope();
            var scopeProvider = diScope.ServiceProvider.GetRequiredService<IScopeProvider>();
            using var scope = scopeProvider.CreateScope(autoComplete: true);

            var total = 0;
            int deleted;
            do
            {
                deleted = await scope.Database.ExecuteAsync(
                    "DELETE TOP (" + BatchSize + ") FROM PageViews WHERE OccurredAt < @0", cutoff);
                total += deleted;
            } while (deleted == BatchSize && !stoppingToken.IsCancellationRequested);

            if (total > 0)
                logger.LogInformation("{Count} page views older than {Cutoff:yyyy-MM-dd} purged.", total, cutoff);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Purging old page views failed.");
        }
    }
}

public sealed class PageViewPurgeComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder) =>
        builder.Services.AddHostedService<PageViewPurge>();
}
