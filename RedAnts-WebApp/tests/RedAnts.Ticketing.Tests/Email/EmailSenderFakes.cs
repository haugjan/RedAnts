using RedAnts.Ticketing.Features.Email;

namespace RedAnts.Ticketing.Tests.Email;

internal sealed class RecordingEmailSender : IEmailSender
{
    public List<(string To, string Subject, string Html)> Sent { get; } = [];

    public Task<EmailSendResult> SendAsync(string toEmail, string? toName, string subject, string htmlBody, CancellationToken cancellationToken = default) =>
        SendAsync(toEmail, toName, subject, htmlBody, null, cancellationToken);

    public Task<EmailSendResult> SendAsync(string toEmail, string? toName, string subject, string htmlBody, IReadOnlyList<EmailAttachment>? attachments,
        CancellationToken cancellationToken = default, string? source = null, string? reference = null)
    {
        Sent.Add((toEmail, subject, htmlBody));
        return Task.FromResult(new EmailSendResult(true, null));
    }
}
