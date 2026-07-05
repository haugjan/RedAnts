using System.Globalization;
using System.Text;
using RedAnts.Domain.Ticketing.Sales;
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
            var categoryCell = Cell(cells, 0);
            var last = Cell(cells, 1);
            var first = Cell(cells, 2);
            var birthdayCell = Cell(cells, 3);

            if (lineNo == 1 && IsHeader(categoryCell, last, first, birthdayCell)) continue;

            var category = ParseCategory(categoryCell);
            if (category is null)
            {
                if (!string.IsNullOrEmpty(categoryCell))
                    warnings.Add($"Zeile {lineNo}: Kategorie „{categoryCell}“ nicht erkannt (erlaubt: Red Ants, Block 4); Red Ants verwendet.");
                category = MemberCategory.RedAnts;
            }

            DateOnly? birthday = null;
            if (!string.IsNullOrEmpty(birthdayCell))
            {
                if (DateOnly.TryParseExact(birthdayCell, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                    birthday = d;
                else
                    warnings.Add($"Zeile {lineNo}: Geburtsdatum „{birthdayCell}“ nicht erkannt, wird leer übernommen.");
            }

            rows.Add(new MemberImportRow(category.Value, last, first, birthday));
        }
        return new MemberCsvResult(rows, warnings);
    }

    public static byte[] SampleBytes()
    {
        var csv = "Kategorie;Name;Vorname;Geburtsdatum\n" +
                  "Red Ants;Muster;Anna;14.05.1990\n" +
                  "Block 4;Beispiel;Ben;02.11.2009\n" +
                  "Red Ants;Nurnachname;;\n";
        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(csv);
    }

    private static string? Cell(string[] cells, int i) =>
        i < cells.Length && !string.IsNullOrWhiteSpace(cells[i]) ? cells[i].Trim() : null;

    private static MemberCategory? ParseCategory(string? cell)
    {
        var norm = new string((cell ?? "").ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        return norm switch
        {
            "redants" => MemberCategory.RedAnts,
            "block4" => MemberCategory.Block4,
            _ => null
        };
    }

    private static bool IsHeader(string? category, string? last, string? first, string? birthday)
    {
        var joined = $"{category} {last} {first} {birthday}".ToLowerInvariant();
        return joined.Contains("kategorie") || joined.Contains("vorname") || joined.Contains("geburt")
            || string.Equals(last, "name", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record MemberCsvResult(IReadOnlyList<MemberImportRow> Rows, IReadOnlyList<string> Warnings);
