using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace RedAnts.Infrastructure.Ticketing.Payments;

public sealed class PayrexxGateway(
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    ILogger<PayrexxGateway> logger) : IPayrexxGateway
{
    private string? Instance => config["Payrexx:Instance"];
    private string? Secret => config["Payrexx:ApiSecret"];
    private string BaseUrl => (config["Payrexx:BaseUrl"] ?? "https://api.payrexx.com/v1.0/").TrimEnd('/') + "/";

    public bool Enabled => !string.IsNullOrWhiteSpace(Instance) && !string.IsNullOrWhiteSpace(Secret);

    public async Task<PayrexxGatewayResult> CreateGatewayAsync(PayrexxCreateRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enabled) throw new InvalidOperationException("Payrexx ist nicht konfiguriert.");

        var fields = new List<KeyValuePair<string, string>>
        {
            new("amount", request.AmountInCents.ToString(CultureInfo.InvariantCulture)),
            new("currency", request.Currency),
            new("purpose", request.Purpose),
            new("referenceId", request.ReferenceId),
            new("successRedirectUrl", request.SuccessUrl),
            new("failedRedirectUrl", request.FailedUrl),
            new("cancelRedirectUrl", request.CancelUrl),
            new("skipResultPage", "1")
        };
        if (!string.IsNullOrWhiteSpace(request.Email)) fields.Add(new("fields[email][value]", request.Email));
        if (!string.IsNullOrWhiteSpace(request.FirstName)) fields.Add(new("fields[forename][value]", request.FirstName));
        if (!string.IsNullOrWhiteSpace(request.LastName)) fields.Add(new("fields[surname][value]", request.LastName));

        var body = SignedBody(fields);
        var url = $"{BaseUrl}Gateway/?instance={Uri.EscapeDataString(Instance!)}";

        var client = httpClientFactory.CreateClient("payrexx");
        using var content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");
        using var response = await client.PostAsync(url, content, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Payrexx create gateway failed: {Status} {Body}", response.StatusCode, json);
            throw new InvalidOperationException("Payrexx-Zahlung konnte nicht gestartet werden.");
        }

        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data")[0];
        var id = data.GetProperty("id").GetInt64().ToString(CultureInfo.InvariantCulture);
        var link = data.GetProperty("link").GetString() ?? "";
        if (string.IsNullOrEmpty(link)) throw new InvalidOperationException("Payrexx lieferte keinen Zahlungslink.");
        return new PayrexxGatewayResult(id, link);
    }

    public async Task<PayrexxStatus> GetGatewayStatusAsync(string gatewayId, CancellationToken cancellationToken = default)
    {
        if (!Enabled) return PayrexxStatus.Pending;

        var signature = Sign("");
        var url = $"{BaseUrl}Gateway/{Uri.EscapeDataString(gatewayId)}/?instance={Uri.EscapeDataString(Instance!)}&ApiSignature={Uri.EscapeDataString(signature)}";

        var client = httpClientFactory.CreateClient("payrexx");
        using var response = await client.GetAsync(url, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Payrexx retrieve gateway failed: {Status} {Body}", response.StatusCode, json);
            return PayrexxStatus.Pending;
        }

        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data")[0];
        var status = data.GetProperty("status").GetString() ?? "";
        return MapStatus(status);
    }

    private static PayrexxStatus MapStatus(string status) => status.ToLowerInvariant() switch
    {
        "confirmed" or "authorized" or "reserved" => PayrexxStatus.Confirmed,
        "cancelled" => PayrexxStatus.Cancelled,
        "declined" or "error" or "chargeback" or "refunded" or "refund" or "expired" => PayrexxStatus.Declined,
        _ => PayrexxStatus.Pending
    };

    private string SignedBody(IReadOnlyList<KeyValuePair<string, string>> fields)
    {
        var query = string.Join("&", fields.Select(kv => $"{PhpUrlEncode(kv.Key)}={PhpUrlEncode(kv.Value)}"));
        var signature = Sign(query);
        return query + "&ApiSignature=" + PhpUrlEncode(signature);
    }

    private string Sign(string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Secret!));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));
    }

    internal static string PhpUrlEncode(string value)
    {
        var sb = new StringBuilder();
        foreach (var b in Encoding.UTF8.GetBytes(value))
        {
            var c = (char)b;
            if (c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_' or '.')
                sb.Append(c);
            else if (c == ' ')
                sb.Append('+');
            else
                sb.Append('%').Append(b.ToString("X2"));
        }
        return sb.ToString();
    }
}

public sealed class PayrexxGatewayComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddHttpClient("payrexx");
        builder.Services.AddScoped<IPayrexxGateway, PayrexxGateway>();
    }
}
