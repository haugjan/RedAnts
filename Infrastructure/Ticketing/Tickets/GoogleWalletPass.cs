using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using RedAnts.Features.Ticketing.Tickets;

namespace RedAnts.Infrastructure.Ticketing.Tickets;

public sealed class GoogleWalletPass(IConfiguration config) : IWalletPass
{
    private const string ClassSuffix = "redants_generic_v1";
    private const string LogoUrl = "https://redants.ch/uploads/232/admin/website_header/RA_Logo_transparent_300ppi.png";

    private static readonly JsonSerializerOptions JsonOptions =
        new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    private string? IssuerId => config["GoogleWallet:IssuerId"];
    private string? ServiceAccountJson => config["GoogleWallet:ServiceAccountJson"];

    public bool Enabled => !string.IsNullOrWhiteSpace(IssuerId) && !string.IsNullOrWhiteSpace(ServiceAccountJson);

    public string? SaveUrl(WalletTicketModel m, string origin)
    {
        if (!Enabled) return null;

        using var sa = JsonDocument.Parse(ServiceAccountJson!);
        var clientEmail = sa.RootElement.GetProperty("client_email").GetString();
        var privateKey = sa.RootElement.GetProperty("private_key").GetString();
        if (string.IsNullOrEmpty(clientEmail) || string.IsNullOrEmpty(privateKey)) return null;

        var classId = $"{IssuerId}.{ClassSuffix}";
        var objectId = $"{IssuerId}.{m.Uuid:N}";

        var textModules = new List<object>();
        if (m.DateText is not null) textModules.Add(TextModule("Datum", m.DateText));
        if (m.CategoryLabel is not null) textModules.Add(TextModule("Kategorie", m.CategoryLabel));
        if (m.HolderName is not null) textModules.Add(TextModule("Name", m.HolderName));

        var genericObject = new Dictionary<string, object?>
        {
            ["id"] = objectId,
            ["classId"] = classId,
            ["state"] = "ACTIVE",
            ["hexBackgroundColor"] = m.AccentHex,
            ["cardTitle"] = Localized("Red Ants Winterthur"),
            ["header"] = Localized(m.TypeLabel),
            ["subheader"] = Localized(m.ScopeName),
            ["logo"] = Image(LogoUrl),
            ["barcode"] = new Dictionary<string, object?>
            {
                ["type"] = "QR_CODE",
                ["value"] = m.TicketUrl,
                ["alternateText"] = m.TicketRef
            },
            ["textModulesData"] = textModules
        };

        var claims = new Dictionary<string, object?>
        {
            ["iss"] = clientEmail,
            ["aud"] = "google",
            ["typ"] = "savetowallet",
            ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["origins"] = new[] { origin },
            ["payload"] = new Dictionary<string, object?>
            {
                ["genericClasses"] = new object[] { new Dictionary<string, object?> { ["id"] = classId } },
                ["genericObjects"] = new object[] { genericObject }
            }
        };

        return "https://pay.google.com/gp/v/save/" + SignJwt(claims, privateKey);
    }

    private static object TextModule(string header, string body) =>
        new Dictionary<string, object?> { ["header"] = header, ["body"] = body };

    private static object Localized(string value) =>
        new Dictionary<string, object?>
        {
            ["defaultValue"] = new Dictionary<string, object?> { ["language"] = "de", ["value"] = value }
        };

    private static object Image(string uri) =>
        new Dictionary<string, object?>
        {
            ["sourceUri"] = new Dictionary<string, object?> { ["uri"] = uri }
        };

    private static string SignJwt(Dictionary<string, object?> claims, string privateKeyPem)
    {
        var header = new Dictionary<string, object?> { ["alg"] = "RS256", ["typ"] = "JWT" };
        var signingInput = $"{Base64Url(JsonSerializer.SerializeToUtf8Bytes(header, JsonOptions))}." +
                           $"{Base64Url(JsonSerializer.SerializeToUtf8Bytes(claims, JsonOptions))}";
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        var signature = rsa.SignData(Encoding.ASCII.GetBytes(signingInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return $"{signingInput}.{Base64Url(signature)}";
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
