using System.IO.Compression;
using System.Xml.Linq;

namespace RedAnts.Features.Ticketing.Admin;

public static class SpreadsheetReader
{
    public static bool IsXlsx(string fileName) =>
        fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase);

    public static IReadOnlyList<string[]> Read(byte[] bytes)
    {
        using var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);

        var shared = ReadSharedStrings(zip);
        var sheet = FirstWorksheet(zip);
        if (sheet is null) return [];

        using var stream = sheet.Open();
        var doc = XDocument.Load(stream);
        var sheetData = Descendant(doc.Root, "sheetData");
        if (sheetData is null) return [];

        var grid = new List<string[]>();
        foreach (var row in Children(sheetData, "row"))
        {
            var cells = new List<(int Col, string Value)>();
            foreach (var c in Children(row, "c"))
            {
                var col = ColumnIndex(c.Attribute("r")?.Value);
                var value = CellValue(c, shared);
                if (col >= 0 && value.Length > 0) cells.Add((col, value));
            }

            if (cells.Count == 0) { grid.Add([]); continue; }

            var width = cells.Max(x => x.Col) + 1;
            var arr = new string[width];
            for (var i = 0; i < width; i++) arr[i] = "";
            foreach (var (col, value) in cells) arr[col] = value;
            grid.Add(arr);
        }

        return grid;
    }

    private static string CellValue(XElement c, IReadOnlyList<string> shared)
    {
        var type = c.Attribute("t")?.Value;
        if (type == "s")
        {
            var v = Child(c, "v")?.Value;
            return int.TryParse(v, out var idx) && idx >= 0 && idx < shared.Count ? shared[idx] : "";
        }
        if (type is "inlineStr" or "str")
            return AllText(c);
        return Child(c, "v")?.Value?.Trim() ?? "";
    }

    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive zip)
    {
        var entry = zip.GetEntry("xl/sharedStrings.xml");
        if (entry is null) return [];
        using var stream = entry.Open();
        var doc = XDocument.Load(stream);
        return Children(doc.Root, "si").Select(AllText).ToList();
    }

    private static string AllText(XElement el) =>
        string.Concat(el.Descendants().Where(e => e.Name.LocalName == "t").Select(e => e.Value));

    private static ZipArchiveEntry? FirstWorksheet(ZipArchive zip) =>
        zip.Entries
            .Where(e => e.FullName.StartsWith("xl/worksheets/", StringComparison.OrdinalIgnoreCase)
                        && e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e.FullName.Length).ThenBy(e => e.FullName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

    private static int ColumnIndex(string? cellRef)
    {
        if (string.IsNullOrEmpty(cellRef)) return -1;
        var col = 0;
        var any = false;
        foreach (var ch in cellRef)
        {
            if (ch is >= 'A' and <= 'Z') { col = col * 26 + (ch - 'A' + 1); any = true; }
            else if (ch is >= 'a' and <= 'z') { col = col * 26 + (ch - 'a' + 1); any = true; }
            else break;
        }
        return any ? col - 1 : -1;
    }

    private static XElement? Descendant(XElement? root, string local) =>
        root?.Descendants().FirstOrDefault(e => e.Name.LocalName == local);

    private static IEnumerable<XElement> Children(XElement? el, string local) =>
        el?.Elements().Where(e => e.Name.LocalName == local) ?? [];

    private static XElement? Child(XElement? el, string local) =>
        el?.Elements().FirstOrDefault(e => e.Name.LocalName == local);
}
