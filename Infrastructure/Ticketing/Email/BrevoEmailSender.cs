using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RedAnts.Features.Ticketing.Email;

namespace RedAnts.Infrastructure.Ticketing.Email;

public sealed class BrevoEmailSender(
    IConfiguration config,
    IHttpClientFactory httpClientFactory,
    ILogger<BrevoEmailSender> logger) : IEmailTransport
{
    private static readonly JsonSerializerOptions JsonOptions = new()
        { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    public string Name => "Brevo";

    public async Task<EmailSendResult> SendAsync(
        string toEmail, string? toName, string subject, string htmlBody,
        IReadOnlyList<EmailAttachment>? attachments, CancellationToken cancellationToken = default)
    {
        var apiKey = config["Brevo:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return new EmailSendResult(false, "Brevo:ApiKey not configured.");

        var senderName = config["Brevo:SenderName"] ?? "Red Ants Ticketing";
        var senderEmail = config["Brevo:SenderEmail"] ?? "tickets@redants.ch";
        var adminBcc = config["Brevo:AdminBcc"];

        var html = InlineImages(htmlBody, attachments);
        var fileAttachments = attachments?
            .Where(a => string.IsNullOrEmpty(a.ContentId))
            .Select(a => new BrevoAttachment(a.FileName, a.Base64Content))
            .ToArray();

        var payload = new BrevoPayload(
            Sender: new BrevoAddress(senderName, senderEmail),
            To: [new BrevoAddress(toName ?? toEmail, toEmail)],
            Bcc: string.IsNullOrWhiteSpace(adminBcc) ? null : [new BrevoAddress(null, adminBcc)],
            Subject: subject,
            HtmlContent: html,
            TextContent: HtmlToPlainText(html),
            Attachment: fileAttachments?.Length > 0 ? fileAttachments : null,
            TrackClicks: false,
            TrackOpens: false);

        try
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("api-key", apiKey);

            var json = JsonSerializer.Serialize(payload, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.brevo.com/v3/smtp/email", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Brevo API e-mail sent to {Recipient} (subject: {Subject}).", toEmail, subject);
                return new EmailSendResult(true, null);
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("Brevo API e-mail to {Recipient} failed: {Status} {Error}", toEmail, (int)response.StatusCode, error);
            return new EmailSendResult(false, $"HTTP {(int)response.StatusCode}: {error}");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Brevo API e-mail to {Recipient} threw.", toEmail);
            return new EmailSendResult(false, ex.Message);
        }
    }

    private static string InlineImages(string html, IReadOnlyList<EmailAttachment>? attachments)
    {
        if (attachments is null) return html;
        foreach (var a in attachments)
            if (!string.IsNullOrEmpty(a.ContentId))
                html = html.Replace($"cid:{a.ContentId}", $"data:{a.ContentType};base64,{a.Base64Content}");
        return html;
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

    private sealed record BrevoPayload(
        [property: JsonPropertyName("sender")] BrevoAddress Sender,
        [property: JsonPropertyName("to")] BrevoAddress[] To,
        [property: JsonPropertyName("bcc")] BrevoAddress[]? Bcc,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("htmlContent")] string HtmlContent,
        [property: JsonPropertyName("textContent")] string TextContent,
        [property: JsonPropertyName("attachment")] BrevoAttachment[]? Attachment,
        [property: JsonPropertyName("trackClicks")] bool TrackClicks,
        [property: JsonPropertyName("trackOpens")] bool TrackOpens);

    private sealed record BrevoAddress(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("email")] string Email);

    private sealed record BrevoAttachment(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("content")] string Content);
}
