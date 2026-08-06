using System.Globalization;
using System.Text;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.Admin;

public static class MemberCsv
{
    private static readonly string[] DateFormats = ["dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd"];

    private static readonly Dictionary<string, int> LegacyMap = new()
    {
        ["lastname"] = 0, ["firstname"] = 1, ["birthday"] = 2, ["email"] = 3
    };

    public static MemberCsvResult Parse(string content)
    {
        var rows = new List<MemberImportRow>();
        var warnings = new List<string>();
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(content)) return new MemberCsvResult(rows, warnings, errors);

        var lines = content.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var delimiter = content.Contains(';') ? ';' : ',';

        var map = LegacyMap;
        var start = 0;
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].Trim().Length == 0) continue;
            var header = MapHeader(lines[i].Split(delimiter));
            if (header is not null) { map = header; start = i + 1; }
            else start = i;
            break;
        }

        for (var i = start; i < lines.Length; i++)
        {
            var raw = lines[i];
            if (raw.Trim().Length == 0) continue;
            var lineNo = i + 1;
            var cells = raw.Split(delimiter);

            string? Get(string key) => map.TryGetValue(key, out var idx) ? Cell(cells, idx) : null;

            DateOnly? birthday = null;
            var birthdayCell = Get("birthday");
            if (!string.IsNullOrEmpty(birthdayCell))
            {
                if (DateOnly.TryParseExact(birthdayCell, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                    birthday = d;
                else
                    warnings.Add($"Zeile {lineNo}: Geburtsdatum „{birthdayCell}“ nicht erkannt, wird leer übernommen.");
            }

            var email = Get("email");
            if (email is not null && !LooksLikeEmail(email))
                warnings.Add($"Zeile {lineNo}: E-Mail „{email}“ sieht ungültig aus, wird trotzdem übernommen.");

            var admissions = 1;
            var admissionsCell = Get("admissions");
            if (!string.IsNullOrEmpty(admissionsCell))
            {
                if (int.TryParse(admissionsCell, out var n) && n >= 1)
                    admissions = n;
                else
                {
                    errors.Add($"Zeile {lineNo}: Anzahl „{admissionsCell}“ ist ungültig (ganze Zahl ab 1).");
                    continue;
                }
            }

            var address = MemberAddress.Create(Get("salutation"), Get("company"), Get("street"), Get("addressline2"),
                Get("postalcode"), Get("city"), Get("country"), Get("phone"));

            rows.Add(new MemberImportRow(Get("lastname"), Get("firstname"), birthday,
                email, Get("cardno"), address.IsEmpty ? null : address, admissions));
        }
        return new MemberCsvResult(rows, warnings, errors);
    }

    public static byte[] SampleBytes()
    {
        var csv = "Karten-Nr;Anzahl;Firma;Anrede;Name;Vorname;Strasse;Adresszusatz;PLZ;Ort;Land;E-Mail;Telefon;Geburtsdatum\n" +
                  ";1;;Frau;Muster;Anna;Musterweg 1;;8400;Winterthur;Schweiz;anna.muster@example.com;079 000 00 00;14.05.1990\n" +
                  ";5;;Herr;Beispiel;Ben;;;;;;ben.beispiel@example.com;;02.11.2009\n" +
                  ";1;;;Nurnachname;;;;;;;;;\n";
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
    }

    private static Dictionary<string, int>? MapHeader(string[] cells)
    {
        var map = new Dictionary<string, int>();
        for (var i = 0; i < cells.Length; i++)
        {
            var key = HeaderKey(cells[i]);
            if (key is not null && !map.ContainsKey(key)) map[key] = i;
        }
        var recognisable = map.ContainsKey("lastname")
            || map.ContainsKey("firstname") || map.ContainsKey("cardno");
        return recognisable ? map : null;
    }

    private static string? HeaderKey(string cell)
    {
        var norm = new string((cell ?? "").ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        return norm switch
        {
            "kartennr" or "kartennummer" or "karte" => "cardno",
            "anzahl" or "einlässe" or "einlaesse" or "anzahleinlässe" or "anzahleinlaesse" => "admissions",
            "anrede" => "salutation",
            "firma" or "firmenname" => "company",
            "name" or "nachname" => "lastname",
            "vorname" => "firstname",
            "strasse" or "str" => "street",
            "adresszusatz" or "zusatz" => "addressline2",
            "plz" => "postalcode",
            "ort" => "city",
            "land" => "country",
            "email" or "mail" => "email",
            "telefon" or "tel" or "telefonnummer" => "phone",
            "geburtsdatum" or "geburtstag" or "geburt" => "birthday",
            _ => null
        };
    }

    private static bool LooksLikeEmail(string value)
    {
        var e = value.Trim();
        var at = e.IndexOf('@');
        return at > 0 && at < e.Length - 1 && e[(at + 1)..].Contains('.') && !e.EndsWith('.');
    }

    private static string? Cell(string[] cells, int i) =>
        i < cells.Length && !string.IsNullOrWhiteSpace(cells[i]) ? cells[i].Trim() : null;
}

public sealed record MemberCsvResult(IReadOnlyList<MemberImportRow> Rows, IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors);
