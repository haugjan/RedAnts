using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Tickets;

namespace RedAnts.Infrastructure.Ticketing.Tickets;

public sealed class TicketTokenService : ITicketTokens
{
    private const string Scheme = "RA1";
    private const int SignatureBytes = 16;
    private readonly byte[] _key;

    public TicketTokenService(IConfiguration config, IHostEnvironment environment, ILogger<TicketTokenService> logger)
    {
        var configured = config["Tickets:QrSecret"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            _key = Encoding.UTF8.GetBytes(configured);
        }
        else if (environment.IsDevelopment())
        {
            var seed = config["Umbraco:CMS:Global:Id"] ?? "redants-dev-qr-fallback";
            _key = SHA256.HashData(Encoding.UTF8.GetBytes("RedAnts.Tickets.Qr:" + seed));
            logger.LogWarning("Tickets:QrSecret is not configured; using a derived development key. " +
                              "Set Tickets:QrSecret in production so QR tickets cannot be forged.");
        }
        else
        {
            throw new InvalidOperationException(
                "Tickets:QrSecret is not configured. Set it (app setting Tickets__QrSecret) so QR tickets cannot be forged.");
        }
    }

    public string Create(TicketType type, Guid uuid, int scopeId)
    {
        var iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = $"{(int)type}|{uuid:N}|{scopeId}|{iat}";
        var payloadB64 = Base64Url(Encoding.UTF8.GetBytes(payload));
        var sig = Base64Url(Sign(Scheme + "." + payloadB64).AsSpan(0, SignatureBytes).ToArray());
        return $"{Scheme}.{payloadB64}.{sig}";
    }

    public bool TryVerify(string token, out TicketTokenData data)
    {
        data = default!;
        if (string.IsNullOrWhiteSpace(token)) return false;

        var parts = token.Split('.');
        if (parts.Length != 3 || parts[0] != Scheme) return false;

        byte[] expected = Sign(Scheme + "." + parts[1]);
        byte[] provided;
        byte[] payloadBytes;
        try
        {
            provided = FromBase64Url(parts[2]);
            payloadBytes = FromBase64Url(parts[1]);
        }
        catch (FormatException)
        {
            return false;
        }

        if (provided.Length != SignatureBytes) return false;
        if (!CryptographicOperations.FixedTimeEquals(provided, expected.AsSpan(0, SignatureBytes).ToArray()))
            return false;

        var fields = Encoding.UTF8.GetString(payloadBytes).Split('|');
        if (fields.Length != 4) return false;
        if (!int.TryParse(fields[0], out var typeInt)) return false;
        if (!Guid.TryParseExact(fields[1], "N", out var uuid)) return false;
        if (!int.TryParse(fields[2], out var scopeId)) return false;
        if (!long.TryParse(fields[3], out var iat)) return false;
        if (!Enum.IsDefined(typeof(TicketType), typeInt)) return false;

        data = new TicketTokenData((TicketType)typeInt, uuid, scopeId, DateTimeOffset.FromUnixTimeSeconds(iat));
        return true;
    }

    private byte[] Sign(string message)
    {
        using var hmac = new HMACSHA256(_key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromBase64Url(string value)
    {
        var s = value.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }
}
