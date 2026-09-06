namespace RedAnts.Features.Ticketing.Email;

public enum OutboxStatus
{
    Pending = 0,
    Sending = 1,
    Sent = 2,
    Failed = 3
}

public sealed record OutboxEnqueueRequest(
    string ToEmail,
    string? ToName,
    string Subject,
    string HtmlBody,
    IReadOnlyList<EmailAttachment>? Attachments = null,
    string? Source = null,
    string? Reference = null);

public sealed record OutboxMessage(
    int Id,
    string ToEmail,
    string? ToName,
    string Subject,
    string HtmlBody,
    IReadOnlyList<EmailAttachment>? Attachments,
    int Attempts,
    string? SentVia);

public sealed record OutboxEntry(
    int Id,
    string ToEmail,
    string? ToName,
    string Subject,
    OutboxStatus Status,
    int Attempts,
    string? SentVia,
    string? LastError,
    string? Source,
    string? Reference,
    DateTime CreatedAt,
    DateTime NextAttemptAt,
    DateTime? SentAt);

public interface IEmailTransport
{
    string Name { get; }

    Task<EmailSendResult> SendAsync(
        string toEmail,
        string? toName,
        string subject,
        string htmlBody,
        IReadOnlyList<EmailAttachment>? attachments,
        CancellationToken cancellationToken = default);
}

public interface IEmailOutbox
{
    Task EnqueueAsync(OutboxEnqueueRequest request, CancellationToken cancellationToken = default);
    Task<OutboxMessage?> ClaimNextDueAsync(DateTime now, CancellationToken cancellationToken = default);
    Task MarkSentAsync(int id, string sentVia, DateTime sentAt);
    Task RescheduleAsync(int id, string? sentVia, string lastError, DateTime nextAttemptAt);
    Task MarkFailedAsync(int id, string? sentVia, string lastError);
    Task<int> PurgeSentBeforeAsync(DateTime cutoff);
}

public interface IOutboxAdminReport
{
    Task<IReadOnlyList<OutboxEntry>> ListAsync(bool includeSent, DateTime sentSince);
    Task<bool> RequeueAsync(int id);
}
