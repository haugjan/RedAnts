using System.Globalization;
using System.Text;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.Admin;

public static class MemberCsv
{
    private static readonly string[] DateFormats = ["dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd"];

    public static MemberCsvResult Parse(string content)
    {
        var rows = new List<MemberImportRow>();
        var warnings = new List<string>();
        if (string.IsNullOrWhiteSpace(content)) return new MemberCsvResult(rows, warnings);

        var lines = content.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var delimiter = content.Contains(';') ? ';' : ',';

        var lineNo = 0;
        foreach (var raw in lines)
        {
            lineNo++;
            if (raw.Trim().Length == 0) continue;

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

public sealed record MemberCsvResult(IReadOnlyList<MemberImportRow> Rows, IReadOnlyList<string> Warnings);
