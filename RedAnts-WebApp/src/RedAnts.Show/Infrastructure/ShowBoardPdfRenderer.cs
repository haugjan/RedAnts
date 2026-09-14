using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RedAnts.Show.Domain;
using RedAnts.Show.Features.Admin;

namespace RedAnts.Show.Infrastructure;

public sealed partial class ShowBoardPdfRenderer : IShowBoardPdf
{
    private const string Red = "#D02D38";
    private const string Ink = "#14171A";
    private const string Muted = "#6B7178";
    private const string Line = "#E5E7EB";
    private const string HeadBg = "#F4F4F5";

    public byte[] Render(IReadOnlyList<ShowProfile> profiles) =>
        Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(t => t.FontSize(9).FontColor(Ink));

                page.Header().Row(row =>
                {
                    row.RelativeItem().AlignMiddle().Text(t =>
                    {
                        t.Span("RED ANTS ").Bold().FontColor(Red);
                        t.Span("· Show-Konfiguration").FontColor(Muted);
                    });
                    row.AutoItem().AlignMiddle().AlignRight().Text($"Stand {DateTime.Now:dd.MM.yyyy HH:mm}").FontColor(Muted).FontSize(8);
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.CurrentPageNumber().FontColor(Muted).FontSize(8);
                    t.Span(" / ").FontColor(Muted).FontSize(8);
                    t.TotalPages().FontColor(Muted).FontSize(8);
                });

                page.Content().PaddingVertical(6).Column(col =>
                {
                    if (profiles.Count == 0)
                    {
                        col.Item().Text("Keine Profile vorhanden.").Italic().FontColor(Muted);
                        return;
                    }
                    for (var i = 0; i < profiles.Count; i++)
                        RenderProfile(col, profiles[i], first: i == 0);
                });
            });
        }).GeneratePdf();

    private static void RenderProfile(ColumnDescriptor col, ShowProfile profile, bool first)
    {
        var color = string.IsNullOrWhiteSpace(profile.Color) ? Red : profile.Color!;
        var (tiles, songs) = Count(profile.Root);

        col.Item().PaddingTop(first ? 0 : 16).Background(color).Padding(7).Row(row =>
        {
            row.RelativeItem().AlignMiddle().Text(profile.Name).Bold().FontSize(14).FontColor(Colors.White);
            row.AutoItem().AlignMiddle().AlignRight().Text($"{tiles} Kacheln · {songs} Songs").FontColor(Colors.White).FontSize(9);
        });
        col.Item().PaddingTop(4).Column(inner => RenderTiles(inner, profile.Root, 0));
    }

    private static void RenderTiles(ColumnDescriptor col, IReadOnlyList<ShowButton> tiles, int depth)
    {
        foreach (var tile in Ordered(tiles))
        {
            var indent = depth * 14f;

            if (tile.IsControl)
            {
                col.Item().PaddingLeft(indent).PaddingTop(5).Text(t =>
                {
                    t.Span($"{ShowControls.Icon(tile.Control!.Value)} {tile.Label}").FontColor(Muted).FontSize(9.5f);
                    t.Span($"   Steuerung: {ShowControls.Label(tile.Control!.Value)}").FontColor(Muted).FontSize(8.5f);
                });
                continue;
            }

            if (tile.IsFolder)
            {
                col.Item().PaddingLeft(indent).PaddingTop(9).BorderBottom(1).BorderColor(Line).PaddingBottom(2).Text(t =>
                {
                    t.Span($"📁 {tile.Label}").Bold().FontSize(11).FontColor(Red);
                    if (!string.IsNullOrWhiteSpace(tile.Subtitle))
                        t.Span($"  {tile.Subtitle}").FontColor(Muted).FontSize(9);
                });
                RenderTiles(col, tile.Children!, depth + 1);
                continue;
            }

            var list = tile.EffectiveSongs;
            col.Item().PaddingLeft(indent).PaddingTop(8).Text(t =>
            {
                t.Span(tile.Label).Bold().FontSize(10).FontColor(Ink);
                if (!string.IsNullOrWhiteSpace(tile.Subtitle))
                    t.Span($"  ({tile.Subtitle})").FontColor(Muted).FontSize(9);
                if (tile.SongsRandom && list.Count > 1)
                    t.Span("  · Zufallswiedergabe").FontColor(Muted).FontSize(8.5f);
            });

            if (list.Count == 0)
            {
                col.Item().PaddingLeft(indent + 8).Text("(kein Song hinterlegt)").Italic().FontColor(Muted).FontSize(8.5f);
                continue;
            }

            col.Item().PaddingLeft(indent + 8).PaddingTop(3).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(22);
                    c.RelativeColumn(3);
                    c.RelativeColumn(2);
                    c.ConstantColumn(62);
                    c.ConstantColumn(52);
                });
                table.Header(header =>
                {
                    HeadCell(header.Cell(), "#");
                    HeadCell(header.Cell(), "Titel");
                    HeadCell(header.Cell(), "Interpret / Quelle");
                    HeadCell(header.Cell(), "Cue-Start");
                    HeadCell(header.Cell(), "Dauer");
                });
                var n = 1;
                foreach (var s in list)
                {
                    BodyCell(table.Cell(), n.ToString(), Muted);
                    BodyCell(table.Cell(), SongTitle(s), Ink);
                    BodyCell(table.Cell(), SongSource(s), Muted);
                    BodyCell(table.Cell(), Cue(s.StartSec), Ink);
                    BodyCell(table.Cell(), s.DurationSec is > 0 ? Cue(s.DurationSec.Value) : "", Muted);
                    n++;
                }
            });
        }
    }

    private static void HeadCell(IContainer cell, string text) =>
        cell.Background(HeadBg).BorderBottom(1).BorderColor(Line).PaddingVertical(3).PaddingHorizontal(4)
            .Text(text).Bold().FontSize(8).FontColor(Muted);

    private static void BodyCell(IContainer cell, string text, string color) =>
        cell.BorderBottom(1).BorderColor(Line).PaddingVertical(2.5f).PaddingHorizontal(4)
            .Text(text).FontSize(8.5f).FontColor(color);

    private static IEnumerable<ShowButton> Ordered(IReadOnlyList<ShowButton> tiles) =>
        tiles
            .OrderBy(t => t.Y < 0 ? int.MaxValue : t.Y)
            .ThenBy(t => t.X < 0 ? int.MaxValue : t.X);

    private static (int Tiles, int Songs) Count(IReadOnlyList<ShowButton> tiles)
    {
        var t = 0;
        var s = 0;
        foreach (var tile in tiles)
        {
            if (tile.IsControl) continue;
            if (tile.IsFolder)
            {
                var (ct, cs) = Count(tile.Children!);
                t += ct;
                s += cs;
                continue;
            }
            t++;
            s += tile.EffectiveSongs.Count;
        }
        return (t, s);
    }

    private static string SongTitle(ShowSound s) =>
        s.Kind == SoundKind.Spotify
            ? (!string.IsNullOrWhiteSpace(s.Title) ? s.Title! : SpotifyId(s.Ref))
            : LocalName(s.Ref);

    private static string SongSource(ShowSound s) =>
        s.Kind == SoundKind.Spotify
            ? (!string.IsNullOrWhiteSpace(s.Artist) ? s.Artist! : "Spotify")
            : "Audiodatei";

    private static string SpotifyId(string reference)
    {
        var parts = reference.Split(':');
        return parts.Length > 0 ? parts[^1] : reference;
    }

    private static string LocalName(string reference)
    {
        var name = reference;
        var slash = name.LastIndexOf('/');
        if (slash >= 0) name = name[(slash + 1)..];
        var dot = name.LastIndexOf('.');
        if (dot > 0) name = name[..dot];
        name = HashSuffix().Replace(name, "");
        name = name.Replace("---", " - ").Replace("__", " ").Replace('_', ' ').Replace('-', ' ');
        name = MultiSpace().Replace(name, " ").Trim();
        return string.IsNullOrWhiteSpace(name) ? reference : name;
    }

    private static string Cue(double seconds)
    {
        if (seconds <= 0) return "0:00";
        var minutes = (int)(seconds / 60);
        var rest = seconds - minutes * 60;
        return $"{minutes}:{rest:00.0}";
    }

    [GeneratedRegex(@"-[0-9a-f]{6,}$")]
    private static partial Regex HashSuffix();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex MultiSpace();
}
