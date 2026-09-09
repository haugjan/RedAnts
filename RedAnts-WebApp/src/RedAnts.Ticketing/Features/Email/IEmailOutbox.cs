namespace RedAnts.Ticketing.Features.Email;

public interface IEmailOutbox
{
    Task EnqueueAsync(OutboxEnqueueRequest request, CancellationToken cancellationToken = default);
    Task<OutboxMessage?> ClaimNextDueAsync(DateTimeOffset now, CancellationToken cancellationToken = default);
    Task MarkSentAsync(int id, string sentVia, DateTimeOffset sentAt);
    Task RescheduleAsync(int id, string? sentVia, string lastError, DateTimeOffset nextAttemptAt);
    Task MarkFailedAsync(int id, string? sentVia, string lastError);
    Task<int> PurgeSentBeforeAsync(DateTimeOffset cutoff);
}
