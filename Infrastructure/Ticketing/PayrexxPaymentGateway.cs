using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Infrastructure.Ticketing;

/// <summary>
/// Payrexx payment gateway adapter. Creates a hosted payment page ("Gateway") and returns its link.
/// Payrexx signs every request with an HMAC-SHA256 ApiSignature over the URL-encoded form body.
/// When no credentials are configured (local dev), <see cref="IsConfigured"/> is false and the
/// caller falls back to a local payment simulation.
/// </summary>
public sealed class PayrexxPaymentGateway(IHttpClientFactory httpClientFactory, IConfiguration config) : IPaymentGateway
{
    private const string BaseUrl = "https://api.payrexx.com/v1.0/";

    private string Instance => config["Payrexx:Instance"] ?? "";
    private string ApiSecret => config["Payrexx:ApiSecret"] ?? "";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Instance) && !string.IsNullOrWhiteSpace(ApiSecret);

    public async Task<PaymentCreation> CreatePaymentAsync(PaymentRequest request)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Payrexx ist nicht konfiguriert.");

        // Payrexx expects amounts in the smallest currency unit (Rappen for CHF).
        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["amount"] = ((int)Math.Round(request.Amount * 100m)).ToString(),
            ["currency"] = request.Currency,
            ["purpose"] = request.Purpose,
            ["referenceId"] = request.ReferenceId,
            ["successRedirectUrl"] = request.SuccessUrl,
            ["failedRedirectUrl"] = request.FailedUrl,
            ["cancelRedirectUrl"] = request.CancelUrl,
            ["email"] = request.Email
        };

        var body = BuildSignedBody(fields);
        var client = httpClientFactory.CreateClient("Payrexx");
        var url = $"{BaseUrl}Gateway/?instance={Uri.EscapeDataString(Instance)}";

        using var content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");
        var response = await client.PostAsync(url, content);
        var json = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data")[0];
        var link = data.GetProperty("link").GetString()
                   ?? throw new InvalidOperationException("Payrexx hat keinen Zahlungslink geliefert.");
        var id = data.TryGetProperty("id", out var idEl) ? idEl.ToString() : null;
        return new PaymentCreation(link, id);
    }

    /// <summary>Builds the URL-encoded form body and appends the Payrexx ApiSignature
    /// (Base64(HMAC-SHA256(unsignedBody, ApiSecret))).</summary>
    private string BuildSignedBody(IEnumerable<KeyValuePair<string, string>> fields)
    {
        var unsigned = string.Join("&", fields.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ApiSecret));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(unsigned)));

        return $"{unsigned}&ApiSignature={Uri.EscapeDataString(signature)}";
    }
}
