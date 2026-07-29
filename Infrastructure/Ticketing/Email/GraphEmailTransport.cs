using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RedAnts.Features.Ticketing.Email;

namespace RedAnts.Infrastructure.Ticketing.Email;

public sealed class GraphEmailTransport(
    IConfiguration config,
    IHttpClientFactory httpClientFactory,
    ILogger<GraphEmailTransport> logger) : IEmailTransport
{
    private static readonly JsonSerializerOptions JsonOptions = new()
        { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    private static readonly SemaphoreSlim TokenLock = new(1, 1);
    private static string? _cachedToken;
    private static string? _cachedForClient;
    private static DateTimeOffset _cachedUntil;

    public string Name => "Graph";

    public async Task<EmailSendResult> SendAsync(
        string toEmail, string? toName, string subject, string htmlBody,
        IReadOnlyList<EmailAttachment>? attachments, CancellationToken cancellationToken = default)
    {
        var tenantId = config["Graph:TenantId"];
        var clientId = config["Graph:ClientId"];
        var clientSecret = config["Graph:ClientSecret"];
        var sender = config["Graph:Sender"] ?? "tickets@redants.ch";
        var saveToSent = string.Equals(config["Graph:SaveToSentItems"], "true", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            return new EmailSendResult(false, "Graph is not configured (Graph:TenantId/ClientId/ClientSecret).");

        var token = await GetTokenAsync(tenantId, clientId, clientSecret, cancellationToken);
        if (token is null) return new EmailSendResult(false, "Graph token acquisition failed (Client-Secret abgelaufen oder ungültig? In Entra erneuern).");

        var payload = BuildPayload(toEmail, toName, subject, htmlBody, attachments, saveToSent);

        try
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"https://graph.microsoft.com/v1.0/users/{Uri.EscapeDataString(sender)}/sendMail";
            var response = await client.PostAsync(url, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Graph e-mail sent to {Recipient} as {Sender}.", toEmail, sender);
                return new EmailSendResult(true, null);
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("Graph e-mail to {Recipient} failed: {Status} {Error}", toEmail, (int)response.StatusCode, error);
            return new EmailSendResult(false, $"HTTP {(int)response.StatusCode}: {error}");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Graph e-mail to {Recipient} threw.", toEmail);
            return new EmailSendResult(false, ex.Message);
        }
    }

    private async Task<string?> GetTokenAsync(string tenantId, string clientId, string clientSecret, CancellationToken ct)
    {
        await TokenLock.WaitAsync(ct);
        try
        {
            if (_cachedToken is not null && _cachedForClient == clientId && _cachedUntil > DateTimeOffset.UtcNow.AddMinutes(2))
                return _cachedToken;

            var client = httpClientFactory.CreateClient();
            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["scope"] = "https://graph.microsoft.com/.default",
                ["grant_type"] = "client_credentials"
            });
            var url = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
            var response = await client.PostAsync(url, form, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Graph token request failed: {Status} {Error}", (int)response.StatusCode, body);
                return null;
            }

            using var doc = JsonDocument.Parse(body);
            var token = doc.RootElement.GetProperty("access_token").GetString();
            var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var e) ? e.GetInt32() : 3600;
            _cachedToken = token;
            _cachedForClient = clientId;
            _cachedUntil = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
            return token;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Graph token acquisition threw.");
            return null;
        }
        finally { TokenLock.Release(); }
    }

    private static GraphSendMail BuildPayload(
        string toEmail, string? toName, string subject, string htmlBody,
        IReadOnlyList<EmailAttachment>? attachments, bool saveToSent)
    {
        var recipients = new[] { new GraphRecipient(new GraphEmailAddress(toName ?? toEmail, toEmail)) };
        var atts = attachments?.Select(a => new GraphAttachment(
            "#microsoft.graph.fileAttachment", a.FileName, a.ContentType, a.Base64Content,
            !string.IsNullOrEmpty(a.ContentId), a.ContentId)).ToArray();
        var message = new GraphMessage(subject, new GraphBody("HTML", htmlBody), recipients,
            atts is { Length: > 0 } ? atts : null);
        return new GraphSendMail(message, saveToSent);
    }

    private sealed record GraphSendMail(
        [property: JsonPropertyName("message")] GraphMessage Message,
        [property: JsonPropertyName("saveToSentItems")] bool SaveToSentItems);

    private sealed record GraphMessage(
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("body")] GraphBody Body,
        [property: JsonPropertyName("toRecipients")] GraphRecipient[] ToRecipients,
        [property: JsonPropertyName("attachments")] GraphAttachment[]? Attachments);

    private sealed record GraphBody(
        [property: JsonPropertyName("contentType")] string ContentType,
        [property: JsonPropertyName("content")] string Content);

    private sealed record GraphRecipient(
        [property: JsonPropertyName("emailAddress")] GraphEmailAddress EmailAddress);

    private sealed record GraphEmailAddress(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("address")] string Address);

    private sealed record GraphAttachment(
        [property: JsonPropertyName("@odata.type")] string ODataType,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("contentType")] string ContentType,
        [property: JsonPropertyName("contentBytes")] string ContentBytes,
        [property: JsonPropertyName("isInline")] bool IsInline,
        [property: JsonPropertyName("contentId")] string? ContentId);
}
