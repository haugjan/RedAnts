using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using RedAnts.Features.Ticketing.Email;

namespace RedAnts.Infrastructure.Ticketing.Email;

public sealed class SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public Task<EmailSendResult> SendAsync(
        string toEmail, string? toName, string subject, string htmlBody, CancellationToken cancellationToken = default)
        => SendAsync(toEmail, toName, subject, htmlBody, null, cancellationToken);

    public async Task<EmailSendResult> SendAsync(
        string toEmail, string? toName, string subject, string htmlBody,
        IReadOnlyList<EmailAttachment>? attachments, CancellationToken cancellationToken = default)
    {
        var host = config["Smtp:Host"];
        var user = config["Smtp:User"];
        var password = config["Smtp:Password"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
            return new EmailSendResult(false, "SMTP is not configured (Smtp:Host/User/Password).");

        var port = int.TryParse(config["Smtp:Port"], out var parsedPort) ? parsedPort : 587;
        var message = BuildMessage(toEmail, toName, subject, htmlBody, attachments);

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, cancellationToken);
            await client.AuthenticateAsync(user, password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            logger.LogInformation("SMTP e-mail sent to {Recipient} (subject: {Subject}).", toEmail, subject);
            return new EmailSendResult(true, null);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SMTP e-mail to {Recipient} failed.", toEmail);
            return new EmailSendResult(false, ex.Message);
        }
    }

    public MimeMessage BuildMessage(
        string toEmail, string? toName, string subject, string htmlBody,
        IReadOnlyList<EmailAttachment>? attachments)
    {
        var fromEmail = config["Smtp:From"] ?? "tickets@redants.ch";
        var fromName = config["Smtp:FromName"] ?? "Red Ants Ticketing";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress(toName ?? toEmail, toEmail));

        var adminBcc = config["Smtp:AdminBcc"];
        if (!string.IsNullOrWhiteSpace(adminBcc) && MailboxAddress.TryParse(adminBcc, out var bcc))
            message.Bcc.Add(bcc);

        message.Subject = subject;

        var builder = new BodyBuilder { HtmlBody = htmlBody, TextBody = HtmlToPlainText(htmlBody) };
        if (attachments is { Count: > 0 })
        {
            foreach (var attachment in attachments)
            {
                byte[] bytes;
                try
                {
                    bytes = Convert.FromBase64String(attachment.Base64Content);
                }
                catch (FormatException)
                {
                    logger.LogWarning("Skipped attachment {File}: invalid base64.", attachment.FileName);
                    continue;
                }

                if (string.IsNullOrEmpty(attachment.ContentId))
                {
                    builder.Attachments.Add(attachment.FileName, bytes);
                }
                else
                {
                    var inline = builder.LinkedResources.Add(
                        attachment.FileName, bytes, ContentType.Parse(attachment.ContentType));
                    inline.ContentId = attachment.ContentId;
                    inline.ContentType.Name = null;
                    inline.ContentDisposition = new ContentDisposition(ContentDisposition.Inline) { FileName = null };
                    if (inline is MimePart part) part.ContentTransferEncoding = ContentEncoding.Base64;
                }
            }
        }
        message.Body = builder.ToMessageBody();
        return message;
    }

    private static string HtmlToPlainText(string html)
    {
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<(br|BR)\\s*/?>", "\n");
        text = System.Text.RegularExpressions.Regex.Replace(text, "</(p|P|tr|TR|div|DIV|h1|h2|h3)>", "\n");
        text = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]+>", "");
        text = System.Net.WebUtility.HtmlDecode(text);
        text = System.Text.RegularExpressions.Regex.Replace(text, "[ \\t]+", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, "\\n{3,}", "\n\n");
        return text.Trim();
    }
}
