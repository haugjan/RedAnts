using System.Text;
using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.Admin;

public static class SeasonPassCsv
{
    public static SeasonPassCsvResult Parse(string content)
    {
        var rows = new List<SeasonPassImportRow>();
        var warnings = new List<string>();
        if (string.IsNullOrWhiteSpace(content)) return new SeasonPassCsvResult(rows, warnings);

        var lines = content.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var delimiter = content.Contains(';') ? ';' : ',';

        var lineNo = 0;
        foreach (var raw in lines)
        {
            lineNo++;
            if (raw.Trim().Length == 0) continue;

            var c = raw.Split(delimiter);
            var bundle = Cell(c, 0);
            var categoryCell = Cell(c, 1);
            var firma = Cell(c, 2);
            var vorname = Cell(c, 3);
            var nachname = Cell(c, 4);

            if (lineNo == 1 && IsHeader(bundle, categoryCell, firma, vorname)) continue;

            if (string.IsNullOrEmpty(bundle))
            {
                warnings.Add($"Zeile {lineNo}: Bundle fehlt, Zeile übersprungen.");
                continue;
            }

            var category = ParseCategory(categoryCell) ?? TicketCategory.Adult;

            Buyer buyer;
            try
            {
                buyer = !string.IsNullOrEmpty(firma)
                    ? Buyer.Create(BuyerType.Company, null, null, firma)
                    : Buyer.Create(BuyerType.Private, vorname, nachname, null);
            }
            catch (DomainException)
            {
                warnings.Add($"Zeile {lineNo}: Käufername fehlt (Firma oder Vor- und Nachname), Zeile übersprungen.");
                continue;
            }

            var address = new SeasonPassImportAddress(
                Cell(c, 5), Cell(c, 6), Cell(c, 7), Cell(c, 8), Cell(c, 9), Cell(c, 10));

            rows.Add(new SeasonPassImportRow(bundle, category, buyer, address));
        }
        return new SeasonPassCsvResult(rows, warnings);
    }

    public static byte[] SampleBytes()
    {
        var csv = "Bundle;Kategorie;Firma;Vorname;Nachname;Strasse;PLZ;Ort;Land;E-Mail;Telefon\n" +
                  "Sponsoren 2026;Erwachsen;;Anna;Muster;Musterweg 1;8400;Winterthur;Schweiz;anna@example.ch;079 000 00 00\n" +
                  "Sponsoren 2026;Erwachsen;Beispiel AG;;;Bahnhofstrasse 5;8400;Winterthur;Schweiz;info@beispiel.ch;\n" +
                  "Gönner;Jugend;;Ben;Beispiel;;;;;;\n";
        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(csv);
    }

    private static string? Cell(string[] cells, int i) =>
        i < cells.Length && !string.IsNullOrWhiteSpace(cells[i]) ? cells[i].Trim() : null;

    private static TicketCategory? ParseCategory(string? cell)
    {
        var n = new string((cell ?? "").ToLowerInvariant().Where(char.IsLetter).ToArray());
        if (n.Length == 0) return null;
        var promo = n.Contains("sonderaktion") || n.Contains("promo") || n.Contains("aktion");
        if (n.Contains("kind")) return TicketCategory.Child;
        if (n.Contains("jugend")) return promo ? TicketCategory.YouthPromo : TicketCategory.Youth;
        if (n.Contains("erwachsen") || n.Contains("adult")) return promo ? TicketCategory.AdultPromo : TicketCategory.Adult;
        return null;
    }

    private static bool IsHeader(string? bundle, string? category, string? firma, string? vorname)
    {
        var j = $"{bundle} {category} {firma} {vorname}".ToLowerInvariant();
        return j.Contains("bundle") || j.Contains("kategorie") || j.Contains("firma") || j.Contains("vorname");
    }
}

public sealed record SeasonPassCsvResult(IReadOnlyList<SeasonPassImportRow> Rows, IReadOnlyList<string> Warnings);
