namespace RedAnts.Features.Ticketing.Email;

public sealed record EmailSendResult(bool Success, string? Error);

public sealed record EmailAttachment(
    string FileName,
    string Base64Content,
    string ContentType = "application/octet-stream",
    string? ContentId = null);

public interface IEmailSender
{
    Task<EmailSendResult> SendAsync(
        string toEmail,
        string? toName,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);

    Task<EmailSendResult> SendAsync(
        string toEmail,
        string? toName,
        string subject,
        string htmlBody,
        IReadOnlyList<EmailAttachment>? attachments,
        CancellationToken cancellationToken = default);
}
