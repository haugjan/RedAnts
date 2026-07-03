using System.Globalization;
using System.Text;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.Admin;

/// <summary>Parses the member-import CSV (columns: Name; Vorname; Geburtsdatum) and produces the sample
/// template. Tolerant by design: any field, or a whole row, may be empty and is still imported. Accepts
/// ';' or ',' as the delimiter and dd.MM.yyyy or yyyy-MM-dd dates; a header row is skipped.</summary>
public static class MemberCsv
{
    private static readonly string[] DateFormats = ["dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd"];

    public static MemberCsvResult Parse(string content)
    {
        var rows = new List<MemberImportRow>();
        var warnings = new List<string>();
        if (string.IsNullOrWhiteSpace(content)) return new MemberCsvResult(rows, warnings);

        var lines = content.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        // Swiss Excel defaults to ';'; fall back to ',' when no semicolon is present.
        var delimiter = content.Contains(';') ? ';' : ',';

        var lineNo = 0;
        foreach (var raw in lines)
        {
            lineNo++;
            if (raw.Trim().Length == 0) continue; // skip truly empty lines (a ";;" line is kept as a blank row)

            var cells = raw.Split(delimiter);
            var last = Cell(cells, 0);
            var first = Cell(cells, 1);
            var birthdayCell = Cell(cells, 2);

            if (lineNo == 1 && IsHeader(last, first, birthdayCell)) continue;

            DateOnly? birthday = null;
            if (!string.IsNullOrEmpty(birthdayCell))
            {
                if (DateOnly.TryParseExact(birthdayCell, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                    birthday = d;
                else
                    warnings.Add($"Zeile {lineNo}: Geburtsdatum „{birthdayCell}“ nicht erkannt, wird leer übernommen.");
            }

            rows.Add(new MemberImportRow(last, first, birthday));
        }
        return new MemberCsvResult(rows, warnings);
    }

    /// <summary>Sample template bytes, UTF-8 with BOM so Excel renders umlauts correctly.</summary>
    public static byte[] SampleBytes()
    {
        var csv = "Name;Vorname;Geburtsdatum\n" +
                  "Muster;Anna;14.05.1990\n" +
                  "Beispiel;Ben;02.11.2009\n" +
                  "Nurnachname;;\n";
        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(csv);
    }

    private static string? Cell(string[] cells, int i) =>
        i < cells.Length && !string.IsNullOrWhiteSpace(cells[i]) ? cells[i].Trim() : null;

    private static bool IsHeader(string? last, string? first, string? birthday)
    {
        var joined = $"{last} {first} {birthday}".ToLowerInvariant();
        return joined.Contains("vorname") || joined.Contains("geburt")
            || string.Equals(last, "name", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>Result of parsing a member CSV: the importable rows plus non-fatal warnings (e.g. an
/// unrecognised date that was kept empty).</summary>
public sealed record MemberCsvResult(IReadOnlyList<MemberImportRow> Rows, IReadOnlyList<string> Warnings);
