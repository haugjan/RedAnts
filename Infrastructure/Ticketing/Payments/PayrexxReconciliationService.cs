using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace RedAnts.Infrastructure.Ticketing.Payments;

public sealed class PayrexxReconciliationService(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    ILogger<PayrexxReconciliationService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(45);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(StartupDelay, stoppingToken); } catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ReconcileAsync(stoppingToken); }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { logger.LogWarning(ex, "Payrexx reconciliation pass failed."); }

            try { await Task.Delay(Interval, stoppingToken); } catch (OperationCanceledException) { break; }
        }
    }

    private async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config["Payrexx:Instance"]) || string.IsNullOrWhiteSpace(config["Payrexx:ApiSecret"]))
            return;

        var baseUrl = config["Tickets:PublicBaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl)) return;

        IReadOnlyList<string> pending;
        using (var scope = scopeFactory.CreateScope())
        {
            var orders = scope.ServiceProvider.GetRequiredService<IOrders>();
            pending = await orders.GetPendingPayrexxOrderNumbersAsync();
        }
        if (pending.Count == 0) return;

        var client = httpClientFactory.CreateClient("payrexx-reconcile");
        foreach (var orderNumber in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var content = new FormUrlEncodedContent(
                    [new KeyValuePair<string, string>("transaction[referenceId]", orderNumber)]);
                using var response = await client.PostAsync($"{baseUrl}/payrexx/webhook", content, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Payrexx reconciliation for {Order} failed.", orderNumber);
            }
        }
    }
}

public sealed class PayrexxReconciliationComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddHttpClient("payrexx-reconcile");
        builder.Services.AddHostedService<PayrexxReconciliationService>();
    }
}
