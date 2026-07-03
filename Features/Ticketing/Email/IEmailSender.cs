namespace RedAnts.Features.Ticketing.Email;

/// <summary>Outcome of a send attempt. <see cref="Error"/> carries the provider message on failure.</summary>
public sealed record EmailSendResult(bool Success, string? Error);

/// <summary>Sends a single transactional HTML e-mail. The caller builds the HTML body (e.g. via
/// <c>EmailLayout.Render</c>); this port only delivers it through the configured provider.</summary>
public interface IEmailSender
{
    Task<EmailSendResult> SendAsync(
        string toEmail,
        string? toName,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);
}
