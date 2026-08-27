using System.Globalization;
using System.Text;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.Admin;

public static class MemberCsv
{
    private static readonly string[] DateFormats = ["dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd"];

    public static readonly IReadOnlyList<MemberField> Fields =
    [
        new("admissions", "Anzahl Einlässe"),
        new("company", "Firma"),
        new("salutation", "Anrede"),
        new("lastname", "Name"),
        new("firstname", "Vorname"),
        new("street", "Strasse"),
        new("addressline2", "Adresszusatz"),
        new("postalcode", "PLZ"),
        new("city", "Ort"),
        new("country", "Land"),
        new("email", "E-Mail"),
        new("phone", "Telefon"),
        new("birthday", "Geburtsdatum"),
    ];

    public static MemberTable ReadTable(byte[] bytes, string fileName)
    {
        var grid = SpreadsheetReader.IsXlsx(fileName) ? SpreadsheetReader.Read(bytes) : ReadCsv(bytes);

        var headerIndex = -1;
        for (var i = 0; i < grid.Count; i++)
            if (grid[i].Count(c => !string.IsNullOrWhiteSpace(c)) >= 2) { headerIndex = i; break; }
        if (headerIndex < 0)
            for (var i = 0; i < grid.Count; i++)
                if (grid[i].Any(c => !string.IsNullOrWhiteSpace(c))) { headerIndex = i; break; }
        if (headerIndex < 0) return new MemberTable([], [], new Dictionary<string, int>());

        var header = grid[headerIndex];
        var columns = new List<string>();
        for (var i = 0; i < header.Length; i++)
            columns.Add(string.IsNullOrWhiteSpace(header[i]) ? $"Spalte {i + 1}" : header[i].Trim());

        var rows = new List<IReadOnlyList<string>>();
        for (var i = headerIndex + 1; i < grid.Count; i++)
            if (grid[i].Any(c => !string.IsNullOrWhiteSpace(c))) rows.Add(grid[i]);

        return new MemberTable(columns, rows, AutoMap(header));
    }

    public static bool EssentialMapped(IReadOnlyDictionary<string, int> map) =>
        map.ContainsKey("lastname") || map.ContainsKey("firstname") || map.ContainsKey("company");

    public static Dictionary<string, int> AutoMap(IReadOnlyList<string> header)
    {
        var map = new Dictionary<string, int>();
        for (var i = 0; i < header.Count; i++)
        {
            var key = HeaderKey(header[i]);
            if (key is not null && !map.ContainsKey(key)) map[key] = i;
        }
        return map;
    }

    public static MemberCsvResult BuildRows(MemberTable table, IReadOnlyDictionary<string, int> map)
    {
        var rows = new List<MemberImportRow>();
        var warnings = new List<string>();
        var errors = new List<string>();

        for (var r = 0; r < table.Rows.Count; r++)
        {
            var cells = table.Rows[r];
            var lineNo = r + 2;

            string? Get(string key) => map.TryGetValue(key, out var idx) && idx >= 0 && idx < cells.Count
                && !string.IsNullOrWhiteSpace(cells[idx])
                ? cells[idx].Trim()
                : null;

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

            var address = MemberAddress.Create(NormalizeSalutation(Get("salutation")), Get("company"), Get("street"),
                Get("addressline2"), Get("postalcode"), Get("city"), Get("country"), Get("phone"));

            var lastName = Get("lastname");
            var firstName = Get("firstname");
            if (lastName is null && firstName is null && email is null && address.IsEmpty) continue;

            rows.Add(new MemberImportRow(lastName, firstName, birthday, email, Get("cardno"),
                address.IsEmpty ? null : address, admissions));
        }

        return new MemberCsvResult(rows, warnings, errors);
    }

    public static byte[] SampleBytes()
    {
        var csv = "Anzahl;Firma;Anrede;Name;Vorname;Strasse;Adresszusatz;PLZ;Ort;Land;E-Mail;Telefon;Geburtsdatum\n" +
                  "1;;Frau;Muster;Anna;Musterweg 1;;8400;Winterthur;Schweiz;anna.muster@example.com;079 000 00 00;14.05.1990\n" +
                  "5;;Herr;Beispiel;Ben;;;;;;ben.beispiel@example.com;;02.11.2009\n" +
                  "1;;;Nurnachname;;;;;;;;;\n";
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
    }

    private static IReadOnlyList<string[]> ReadCsv(byte[] bytes)
    {
        using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var text = reader.ReadToEnd();
        if (string.IsNullOrWhiteSpace(text)) return [];
        var lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var delimiter = text.Contains(';') ? ';' : ',';
        return lines.Select(l => l.Split(delimiter)).ToList();
    }

    private static string? HeaderKey(string? cell)
    {
        var text = cell ?? "";
        var paren = text.IndexOf('(');
        if (paren > 0) text = text[..paren];
        var norm = new string(text.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        if (norm.Length == 0) return null;
        if (norm.Contains("email") || norm is "mail" or "emailadresse") return "email";
        return norm switch
        {
            "kartennr" or "kartennummer" or "karte" => "cardno",
            "anzahl" or "einlässe" or "einlaesse" or "anzahleinlässe" or "anzahleinlaesse" => "admissions",
            "anrede" or "geschlecht" => "salutation",
            "firma" or "firmenname" => "company",
            "name" or "nachname" => "lastname",
            "vorname" => "firstname",
            "strasse" or "str" or "adresse" => "street",
            "adresszusatz" or "zusatz" => "addressline2",
            "plz" or "postleitzahl" => "postalcode",
            "ort" or "wohnort" => "city",
            "land" => "country",
            "telefon" or "tel" or "telefonnummer" or "handy" or "mobile" or "mobil" or "natel" => "phone",
            "geburtsdatum" or "geburtstag" or "geburt" or "gebdatum" => "birthday",
            _ => null
        };
    }

    private static string? NormalizeSalutation(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var v = value.Trim();
        return v.ToLowerInvariant() switch
        {
            "weiblich" or "w" or "f" or "frau" => "Frau",
            "männlich" or "maennlich" or "m" or "herr" => "Herr",
            _ => v
        };
    }

    private static bool LooksLikeEmail(string value)
    {
        var e = value.Trim();
        var at = e.IndexOf('@');
        return at > 0 && at < e.Length - 1 && e[(at + 1)..].Contains('.') && !e.EndsWith('.');
    }
}

public sealed record MemberField(string Key, string Label);

public sealed record MemberTable(
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<string>> Rows,
    IReadOnlyDictionary<string, int> AutoMap);

public sealed record MemberCsvResult(IReadOnlyList<MemberImportRow> Rows, IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors);
