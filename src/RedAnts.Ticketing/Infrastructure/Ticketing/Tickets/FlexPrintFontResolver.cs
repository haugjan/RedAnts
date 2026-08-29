using System.Reflection;
using PdfSharp.Fonts;

namespace RedAnts.Infrastructure.Ticketing.Tickets;

public sealed class FlexPrintFontResolver : IFontResolver
{
    public const string FamilyName = "TicketSans";

    private static readonly byte[] FontData = Load();

    public byte[]? GetFont(string faceName) => FontData;

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        => new(FamilyName);

    private static byte[] Load()
    {
        var assembly = typeof(FlexPrintFontResolver).Assembly;
        var name = Array.Find(assembly.GetManifestResourceNames(),
            n => n.EndsWith("TicketSans.ttf", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Eingebettete Schrift TicketSans.ttf nicht gefunden.");
        using var stream = assembly.GetManifestResourceStream(name)!;
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
