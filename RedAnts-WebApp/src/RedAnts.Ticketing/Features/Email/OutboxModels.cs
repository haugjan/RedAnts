namespace RedAnts.Ticketing.Features.Email;

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
    DateTimeOffset CreatedAt,
    DateTimeOffset NextAttemptAt,
    DateTimeOffset? SentAt);
