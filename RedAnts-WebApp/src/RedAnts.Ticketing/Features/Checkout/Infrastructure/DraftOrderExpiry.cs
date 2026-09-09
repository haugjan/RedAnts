using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Composing;

namespace RedAnts.Ticketing.Features.Checkout.Infrastructure;

public sealed class DraftOrderExpiry(IServiceScopeFactory scopes, ILogger<DraftOrderExpiry> logger) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan MaxDraftAge = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan LookBack = TimeSpan.FromDays(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var expire = scope.ServiceProvider.GetRequiredService<ExpireDraftOrders.Handler>();
                var expired = await expire.HandleAsync(new ExpireDraftOrders.Command(SwissTime.Timestamp - LookBack, SwissTime.Timestamp - MaxDraftAge));
                if (expired > 0)
                    logger.LogInformation("{Count} draft orders expired and their reservations released.", expired);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Expiring draft orders failed.");
            }
        }
    }
}

public sealed class DraftOrderExpiryComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddScoped<RedAnts.Ticketing.Features.Checkout.IOrderTokens, DataProtectionOrderTokens>();
        builder.Services.AddHostedService<DraftOrderExpiry>();
    }
}
