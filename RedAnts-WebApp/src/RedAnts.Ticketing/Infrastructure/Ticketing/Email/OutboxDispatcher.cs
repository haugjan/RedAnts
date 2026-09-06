using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RedAnts.Features.Ticketing.Email;

namespace RedAnts.Infrastructure.Ticketing.Email;

public sealed class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    OutboxSignal signal,
    IConfiguration config,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    private const int MaxAttempts = 6;
    private static readonly TimeSpan Pace = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan FallbackPoll = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan PurgeInterval = TimeSpan.FromHours(1);

    private DateTime _lastPurge = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogSecretExpiryIfNear();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PurgeIfDueAsync();

                using var scope = scopeFactory.CreateScope();
                var outbox = scope.ServiceProvider.GetRequiredService<IEmailOutbox>();
                var selector = scope.ServiceProvider.GetRequiredService<EmailTransportSelector>();

                var message = await outbox.ClaimNextDueAsync(DateTime.UtcNow, stoppingToken);
                if (message is null)
                {
                    await signal.WaitAsync(FallbackPoll, stoppingToken);
                    continue;
                }

                await DeliverAsync(outbox, selector, message, stoppingToken);
                await Task.Delay(Pace, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox dispatcher loop error.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task DeliverAsync(
        IEmailOutbox outbox, EmailTransportSelector selector, OutboxMessage message, CancellationToken cancellationToken)
    {
        var active = selector.Active();
        if (active.Count == 0)
        {
            await outbox.RescheduleAsync(message.Id, message.SentVia, "No e-mail transport configured.",
                DateTime.UtcNow + BackoffFor(message.Attempts));
            return;
        }

        var sent = SplitSentVia(message.SentVia);
        string? lastError = null;

        foreach (var transport in active)
        {
            if (sent.Contains(transport.Name)) continue;

            var subject = active.Count > 1 ? $"[{transport.Name}] {message.Subject}" : message.Subject;
            var result = await transport.SendAsync(
                message.ToEmail, message.ToName, subject, message.HtmlBody, message.Attachments, cancellationToken);

            if (result.Success) sent.Add(transport.Name);
            else lastError = $"{transport.Name}: {result.Error}";
        }

        var sentVia = sent.Count > 0 ? string.Join(",", sent) : null;
        var pending = active.Count(t => !sent.Contains(t.Name));

        if (pending == 0)
            await outbox.MarkSentAsync(message.Id, sentVia ?? "", DateTime.UtcNow);
        else if (message.Attempts >= MaxAttempts)
            await outbox.MarkFailedAsync(message.Id, sentVia, lastError ?? "Delivery failed.");
        else
            await outbox.RescheduleAsync(message.Id, sentVia, lastError ?? "Delivery failed.",
                DateTime.UtcNow + BackoffFor(message.Attempts));
    }

    private async Task PurgeIfDueAsync()
    {
        var now = DateTime.UtcNow;
        if (now - _lastPurge < PurgeInterval) return;
        _lastPurge = now;

        var retentionDays = int.TryParse(config["Email:Outbox:RetentionDays"], out var days) && days > 0 ? days : 30;

        try
        {
            using var scope = scopeFactory.CreateScope();
            var outbox = scope.ServiceProvider.GetRequiredService<IEmailOutbox>();
            var removed = await outbox.PurgeSentBeforeAsync(now.AddDays(-retentionDays));
            if (removed > 0) logger.LogInformation("Outbox purge removed {Count} sent e-mails.", removed);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Outbox purge failed.");
        }
    }

    private void LogSecretExpiryIfNear()
    {
        var raw = config["Graph:ClientSecretExpires"];
        if (!DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var expires)) return;

        var days = (int)Math.Floor((expires.Date - DateTime.UtcNow.Date).TotalDays);
        if (days < 0)
            logger.LogError("Graph client secret expired on {Date:yyyy-MM-dd}. E-mail sending fails until it is renewed.", expires);
        else if (days <= 30)
            logger.LogWarning("Graph client secret expires on {Date:yyyy-MM-dd} (in {Days} days). Renew it in Entra.", expires, days);
    }

    private static HashSet<string> SplitSentVia(string? sentVia) =>
        string.IsNullOrWhiteSpace(sentVia)
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(
                sentVia.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                StringComparer.OrdinalIgnoreCase);

    private static TimeSpan BackoffFor(int attempts)
    {
        var seconds = Math.Min(900d, 60d * Math.Pow(2, Math.Max(0, attempts - 1)));
        return TimeSpan.FromSeconds(seconds);
    }
}
