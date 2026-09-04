using System.Text;
using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Admin;

public static class NewsletterFairgateCsv
{
    private const string Header = "Vorname;Nachname;Primäre E-Mail";

    public static byte[] Build(IEnumerable<NewsletterSignup> signups)
    {
        var sb = new StringBuilder();
        sb.Append(Header).Append("\r\n");
        foreach (var s in signups)
        {
            var (first, last) = SplitName(s.Name);
            sb.Append(Csv(first)).Append(';')
              .Append(Csv(last)).Append(';')
              .Append(Csv(s.Email)).Append("\r\n");
        }
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static (string First, string Last) SplitName(string? name)
    {
        var n = (name ?? "").Trim();
        if (n.Length == 0) return ("", "");
        var idx = n.LastIndexOf(' ');
        return idx < 0 ? (n, "") : (n[..idx].Trim(), n[(idx + 1)..].Trim());
    }

    private static string Csv(string? value)
    {
        var s = Neutralize(value ?? "");
        return s.IndexOfAny([';', '"', '\r', '\n']) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
    }

    private static string Neutralize(string value) =>
        value.Length > 0 && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r' ? "'" + value : value;
}
