using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using RedAnts.Features.Ticketing.Tickets;

namespace RedAnts.Infrastructure.Ticketing.Tickets;

public sealed class MyTicketTokenService(IConfiguration config) : IMyTicketTokens
{
    public string Create(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var key = SecretBytes();
        var payload = Encoding.UTF8.GetBytes(normalized);
        var sig = HMACSHA256.HashData(key, payload)[..16];
        return $"{Base64Url(payload)}.{Base64Url(sig)}";
    }

    public bool TryVerify(string token, out string email)
    {
        email = "";
        if (string.IsNullOrEmpty(token)) return false;
        var dot = token.IndexOf('.');
        if (dot < 1 || dot == token.Length - 1) return false;
        try
        {
            var emailBytes = Base64UrlDecode(token[..dot]);
            var actualSig = Base64UrlDecode(token[(dot + 1)..]);
            if (actualSig.Length != 16) return false;
            email = Encoding.UTF8.GetString(emailBytes);
            var normalized = email.Trim().ToLowerInvariant();
            var expectedSig = HMACSHA256.HashData(SecretBytes(), Encoding.UTF8.GetBytes(normalized))[..16];
            return CryptographicOperations.FixedTimeEquals(expectedSig, actualSig);
        }
        catch { return false; }
    }

    private byte[] SecretBytes()
    {
        var s = config["Ticketing:MyTicketsSecret"] ?? "";
        return Encoding.UTF8.GetBytes(s.Length > 0 ? s : "dev-my-tickets-placeholder");
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        var pad = (4 - s.Length % 4) % 4;
        return Convert.FromBase64String(s + new string('=', pad));
    }
}
