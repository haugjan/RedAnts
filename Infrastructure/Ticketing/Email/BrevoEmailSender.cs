using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RedAnts.Features.Ticketing.Email;

namespace RedAnts.Infrastructure.Ticketing.Email;

/// <summary>Sends transactional e-mail through Brevo's REST API (<c>POST /v3/smtp/email</c>).
/// Configuration (never hardcoded): <c>Brevo:ApiKey</c> (secret — user secret in dev, App Service
/// app setting in prod), <c>Brevo:SenderName</c>, <c>Brevo:SenderEmail</c> (must be a verified Brevo
/// sender), optional <c>Brevo:AdminBcc</c>.</summary>
public sealed class BrevoEmailSender(
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    ILogger<BrevoEmailSender> logger) : IEmailSender
{
    private const string Endpoint = "https://api.brevo.com/v3/smtp/email";

    public async Task<EmailSendResult> SendAsync(
        string toEmail, string? toName, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var apiKey = config["Brevo:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return new EmailSendResult(false, "Brevo:ApiKey is not configured.");

        var senderEmail = config["Brevo:SenderEmail"];
        if (string.IsNullOrWhiteSpace(senderEmail))
            return new EmailSendResult(false, "Brevo:SenderEmail is not configured.");

        var senderName = config["Brevo:SenderName"] ?? "Red Ants Ticketing";

        var payload = new Dictionary<string, object?>
        {
            ["sender"] = new { name = senderName, email = senderEmail },
            ["to"] = new[] { new { email = toEmail, name = toName ?? toEmail } },
            ["subject"] = subject,
            ["htmlContent"] = htmlBody
        };

        var adminBcc = config["Brevo:AdminBcc"];
        if (!string.IsNullOrWhiteSpace(adminBcc))
            payload["bcc"] = new[] { new { email = adminBcc } };

        var client = httpClientFactory.CreateClient("brevo");
        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        request.Headers.Add("api-key", apiKey);
        request.Headers.Add("accept", "application/json");
        request.Content = JsonContent.Create(payload);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Brevo e-mail sent to {Recipient} (subject: {Subject}).", toEmail, subject);
                return new EmailSendResult(true, null);
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("Brevo e-mail to {Recipient} failed: {Status} {Body}", toEmail, (int)response.StatusCode, body);
            return new EmailSendResult(false, $"{(int)response.StatusCode}: {body}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Brevo e-mail to {Recipient} threw.", toEmail);
            return new EmailSendResult(false, ex.Message);
        }
    }
}
