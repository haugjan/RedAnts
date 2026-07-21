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

    public async Task<bool> RefundGatewayAsync(string gatewayId, CancellationToken cancellationToken = default)
    {
        if (!Enabled) return false;

        var (transactionId, amountInCents) = await GetConfirmedTransactionAsync(gatewayId, cancellationToken);
        if (transactionId is null)
        {
            logger.LogError("Payrexx refund: no confirmed transaction for gateway {GatewayId}", gatewayId);
            return false;
        }

        var fields = new List<KeyValuePair<string, string>>
        {
            new("amount", amountInCents.ToString(CultureInfo.InvariantCulture))
        };
        var body = SignedBody(fields);
        var url = $"{BaseUrl}Transaction/{Uri.EscapeDataString(transactionId)}/refund/?instance={Uri.EscapeDataString(Instance!)}";

        var client = httpClientFactory.CreateClient("payrexx");
        using var content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");
        using var response = await client.PostAsync(url, content, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Payrexx refund failed: {Status} {Body}", response.StatusCode, json);
            return false;
        }

        using var doc = JsonDocument.Parse(json);
        var status = doc.RootElement.TryGetProperty("status", out var st) ? st.GetString() : null;
        return string.Equals(status, "success", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<(string? TransactionId, int AmountInCents)> GetConfirmedTransactionAsync(string gatewayId, CancellationToken cancellationToken)
    {
        var signature = Sign("");
        var url = $"{BaseUrl}Gateway/{Uri.EscapeDataString(gatewayId)}/?instance={Uri.EscapeDataString(Instance!)}&ApiSignature={Uri.EscapeDataString(signature)}";

        var client = httpClientFactory.CreateClient("payrexx");
        using var response = await client.GetAsync(url, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Payrexx retrieve gateway for refund failed: {Status} {Body}", response.StatusCode, json);
            return (null, 0);
        }

        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data")[0];
        if (!data.TryGetProperty("invoices", out var invoices) || invoices.ValueKind != JsonValueKind.Array) return (null, 0);

        foreach (var invoice in invoices.EnumerateArray())
        {
            if (!invoice.TryGetProperty("transactions", out var transactions) || transactions.ValueKind != JsonValueKind.Array) continue;
            foreach (var tx in transactions.EnumerateArray())
            {
                var txStatus = tx.TryGetProperty("status", out var s) ? s.GetString() ?? "" : "";
                if (MapStatus(txStatus) != PayrexxStatus.Confirmed) continue;
                if (!tx.TryGetProperty("id", out var idEl)) continue;
                var id = idEl.GetInt64().ToString(CultureInfo.InvariantCulture);
                var amount = tx.TryGetProperty("amount", out var amtEl) && amtEl.TryGetInt32(out var a) ? a : 0;
                return (id, amount);
            }
        }

        return (null, 0);
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
        var signQuery = string.Join("&", fields.Select(kv => $"{EncSign(kv.Key)}={EncSign(kv.Value)}"));
        var bodyQuery = string.Join("&", fields.Select(kv => $"{EncBody(kv.Key)}={EncBody(kv.Value)}"));
        return bodyQuery + "&ApiSignature=" + EncBody(Sign(signQuery));
    }

    private string Sign(string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Secret!));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));
    }

    private static string EncBody(string value) => Uri.EscapeDataString(value);

    private static string EncSign(string value) => Uri.EscapeDataString(value).Replace("%20", "+").Replace("~", "%7E");
}

public sealed class PayrexxGatewayComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddHttpClient("payrexx");
        builder.Services.AddScoped<IPayrexxGateway, PayrexxGateway>();
    }
}
