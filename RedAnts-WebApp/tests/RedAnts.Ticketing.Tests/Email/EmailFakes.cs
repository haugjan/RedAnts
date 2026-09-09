using RedAnts.Ticketing.Features.Email;

namespace RedAnts.Ticketing.Tests.Email;

internal sealed class StubOutboxReader : IOutboxReader
{
    public List<OutboxEntry> Entries { get; } = [];
    public (bool IncludeSent, DateTimeOffset SentSince)? LastCall { get; private set; }

    public Task<IReadOnlyList<OutboxEntry>> ListAsync(bool includeSent, DateTimeOffset sentSince)
    {
        LastCall = (includeSent, sentSince);
        return Task.FromResult<IReadOnlyList<OutboxEntry>>(Entries);
    }

    public static OutboxEntry Entry(int id, OutboxStatus status) =>
        new(id, "anna@example.ch", "Anna", "Betreff", status, 1, null, null, "Test", null, SwissTime.Timestamp, SwissTime.Timestamp, null);
}

internal sealed class RecordingEmailOutbox : IEmailOutbox
{
    public List<int> Requeued { get; } = [];
    public HashSet<int> Requeueable { get; } = [];

    public Task EnqueueAsync(OutboxEnqueueRequest request, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<OutboxMessage?> ClaimNextDueAsync(DateTimeOffset now, CancellationToken cancellationToken = default) =>
        Task.FromResult<OutboxMessage?>(null);

    public Task MarkSentAsync(int id, string sentVia, DateTimeOffset sentAt) => Task.CompletedTask;

    public Task RescheduleAsync(int id, string? sentVia, string lastError, DateTimeOffset nextAttemptAt) => Task.CompletedTask;

    public Task MarkFailedAsync(int id, string? sentVia, string lastError) => Task.CompletedTask;

    public Task<int> PurgeSentBeforeAsync(DateTimeOffset cutoff) => Task.FromResult(0);

    public Task<bool> RequeueAsync(int id)
    {
        Requeued.Add(id);
        return Task.FromResult(Requeueable.Contains(id));
    }
}
