using RedAnts.Features.Ticketing.Email;

namespace RedAnts.Infrastructure.Ticketing.Email;

public sealed class OutboxEnqueuer(IEmailOutbox outbox) : IEmailSender
{
    public Task<EmailSendResult> SendAsync(
        string toEmail, string? toName, string subject, string htmlBody,
        CancellationToken cancellationToken = default)
        => SendAsync(toEmail, toName, subject, htmlBody, null, cancellationToken);

    public async Task<EmailSendResult> SendAsync(
        string toEmail, string? toName, string subject, string htmlBody,
        IReadOnlyList<EmailAttachment>? attachments, CancellationToken cancellationToken = default,
        string? source = null, string? reference = null)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
            return new EmailSendResult(false, "No recipient address.");

        await outbox.EnqueueAsync(
            new OutboxEnqueueRequest(toEmail, toName, subject, htmlBody, attachments, source, reference),
            cancellationToken);
        return new EmailSendResult(true, null);
    }
}
