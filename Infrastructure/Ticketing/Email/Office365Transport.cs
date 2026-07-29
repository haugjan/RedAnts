using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using RedAnts.Features.Ticketing.Email;

namespace RedAnts.Infrastructure.Ticketing.Email;

public sealed class Office365Transport(IConfiguration config, ILogger<Office365Transport> logger) : IEmailTransport
{
    public string Name => "Office365";

    public async Task<EmailSendResult> SendAsync(
        string toEmail, string? toName, string subject, string htmlBody,
        IReadOnlyList<EmailAttachment>? attachments, CancellationToken cancellationToken = default)
    {
        var host = config["Office365:Host"] ?? "smtp.office365.com";
        var user = config["Office365:User"];
        var password = config["Office365:Password"];
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
            return new EmailSendResult(false, "Office365 is not configured (Office365:User/Password).");

        var port = int.TryParse(config["Office365:Port"], out var parsedPort) ? parsedPort : 587;
        var message = BuildMessage(toEmail, toName, subject, htmlBody, attachments);

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, cancellationToken);
            await client.AuthenticateAsync(user, password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            logger.LogInformation("Office365 e-mail sent to {Recipient} (subject: {Subject}).", toEmail, subject);
            return new EmailSendResult(true, null);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Office365 e-mail to {Recipient} failed.", toEmail);
            return new EmailSendResult(false, ex.Message);
        }
    }

    private MimeMessage BuildMessage(
        string toEmail, string? toName, string subject, string htmlBody,
        IReadOnlyList<EmailAttachment>? attachments)
    {
        var fromEmail = config["Office365:From"] ?? config["Office365:User"] ?? "tickets@redants.ch";
        var fromName = config["Office365:FromName"] ?? "Red Ants Ticketing";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress(toName ?? toEmail, toEmail));

        var adminBcc = config["Office365:AdminBcc"];
        if (!string.IsNullOrWhiteSpace(adminBcc) && MailboxAddress.TryParse(adminBcc, out var bcc))
            message.Bcc.Add(bcc);

        message.Subject = subject;

        var alternative = new MultipartAlternative
        {
            new TextPart("plain") { Text = HtmlToPlainText(htmlBody) },
            new TextPart("html") { Text = htmlBody }
        };

        var inlineImages = new List<MimePart>();
        var fileAttachments = new List<MimePart>();
        if (attachments is { Count: > 0 })
        {
            foreach (var attachment in attachments)
            {
                var part = BuildPart(attachment);
                if (part is null) continue;
                if (string.IsNullOrEmpty(attachment.ContentId)) fileAttachments.Add(part);
                else inlineImages.Add(part);
            }
        }

        MimeEntity body = alternative;
        if (inlineImages.Count > 0)
        {
            var related = new MultipartRelated { alternative };
            foreach (var image in inlineImages) related.Add(image);
            related.Root = alternative;
            body = related;
        }
        if (fileAttachments.Count > 0)
        {
            var mixed = new Multipart("mixed") { body };
            foreach (var file in fileAttachments) mixed.Add(file);
            body = mixed;
        }

        message.Body = body;
        return message;
    }

    private MimePart? BuildPart(EmailAttachment attachment)
    {
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(attachment.Base64Content);
        }
        catch (FormatException)
        {
            logger.LogWarning("Skipped attachment {File}: invalid base64.", attachment.FileName);
            return null;
        }

        var isInline = !string.IsNullOrEmpty(attachment.ContentId);
        var part = new MimePart(ContentType.Parse(attachment.ContentType))
        {
            Content = new MimeContent(new MemoryStream(bytes)),
            ContentTransferEncoding = ContentEncoding.Base64,
            ContentDisposition = new ContentDisposition(isInline ? ContentDisposition.Inline : ContentDisposition.Attachment)
        };
        if (isInline)
            part.ContentId = attachment.ContentId;
        else
            part.FileName = attachment.FileName;
        return part;
    }

    private static string HtmlToPlainText(string html)
    {
        const System.Text.RegularExpressions.RegexOptions opts =
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline;
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<(head|style|script)[^>]*>.*?</\\1>", "", opts);
        text = System.Text.RegularExpressions.Regex.Replace(text, "<br\\s*/?>", "\n", opts);
        text = System.Text.RegularExpressions.Regex.Replace(text, "</(p|tr|div|h1|h2|h3)>", "\n", opts);
        text = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]+>", "");
        text = System.Net.WebUtility.HtmlDecode(text);
        text = System.Text.RegularExpressions.Regex.Replace(text, "[ \\t]+", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, "\\n{3,}", "\n\n");
        return text.Trim();
    }
}
