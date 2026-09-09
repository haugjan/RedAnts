namespace RedAnts.Ticketing.Features.Email;

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
