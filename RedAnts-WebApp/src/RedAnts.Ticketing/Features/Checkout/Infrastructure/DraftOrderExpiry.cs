using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Composing;

namespace RedAnts.Ticketing.Features.Checkout.Infrastructure;

public sealed class DraftOrderExpiry(IServiceScopeFactory scopes, TimeProvider time, ILogger<DraftOrderExpiry> logger) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, time);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var expire = scope.ServiceProvider.GetRequiredService<ExpireDraftOrders.Handler>();
                var expired = await expire.HandleAsync(ExpireDraftOrders.Command.Due(time));
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
