using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RedAnts.Features.Ticketing.Tickets;

namespace RedAnts.Infrastructure.Ticketing.Tickets;

public sealed class TicketPdfRenderer(IWebHostEnvironment env) : ITicketPdf
{
    private const string Red = "#D02D38";
    private const string RedDk = "#B0242E";
    private const string Ink = "#14171A";
    private const string Muted = "#6B7178";
    private const string PageBg = "#F4F4F5";

    private const string SeamSvg =
        "<svg viewBox='0 0 240 14' xmlns='http://www.w3.org/2000/svg'>" +
        "<circle cx='0' cy='7' r='7' fill='#F4F4F5'/>" +
        "<circle cx='240' cy='7' r='7' fill='#F4F4F5'/>" +
        "<line x1='16' y1='7' x2='224' y2='7' stroke='#D6DADE' stroke-width='1.2' stroke-dasharray='6 4'/>" +
        "</svg>";

    private readonly byte[]? _logo = LoadLogo(env);

    private static byte[]? LoadLogo(IWebHostEnvironment env)
    {
        try
        {
            var path = Path.Combine(env.WebRootPath ?? "wwwroot", "img", "logo-badge.png");
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }
        catch { return null; }
    }

    public byte[] Render(TicketPdfModel m) =>
        Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(54, 85.6f, Unit.Millimetre);
                page.Margin(2.4f, Unit.Millimetre);
                page.PageColor(PageBg);
                page.DefaultTextStyle(t => t.FontSize(8).FontColor(Ink));
                page.Content()
                    .CornerRadius(4, Unit.Millimetre)
                    .Background(Colors.White)
                    .Column(col =>
                {
                    col.Item().Background(Red).Padding(7).Column(head =>
                    {
                        head.Item().Row(row =>
                        {
                            row.RelativeItem().AlignMiddle().Text(m.Kicker.ToUpperInvariant()).FontColor(Colors.White).FontSize(6.5f);
                            if (_logo is { } logo)
                                row.AutoItem().AlignMiddle()
                                    .Width(10, Unit.Millimetre).Height(10, Unit.Millimetre)
                                    .Image(logo).FitArea();
                            else
                                row.AutoItem().AlignRight().Text("RED ANTS").FontColor(Colors.White).Bold().FontSize(6.5f);
                        });
                        head.Item().PaddingTop(1).Text(m.TypeLabel.ToUpperInvariant()).FontColor(Colors.White).Bold().FontSize(13);
                    });

                    col.Item().PaddingTop(6).AlignCenter().Width(26, Unit.Millimetre).Image(m.QrPng);
                    col.Item().PaddingTop(2).AlignCenter().Text("Am Eingang scannen lassen").FontColor(Muted).FontSize(6.5f);

                    col.Item().PaddingVertical(3).Svg(SeamSvg);

                    col.Item().PaddingHorizontal(13).Text(m.ScopeName).Bold().FontSize(9.5f).FontColor(Ink);
                    col.Item().PaddingHorizontal(13).PaddingTop(2).PaddingBottom(2).Column(meta =>
                    {
                        if (m.DateText is not null) MetaRow(meta, "Datum", m.DateText, RedDk);
                        if (m.VenueName is not null) MetaRow(meta, "Ort", m.VenueName, Ink);
                        if (m.CategoryLabel is not null) MetaRow(meta, "Kategorie", m.CategoryLabel, Ink);
                        if (m.HolderName is not null) MetaRow(meta, "Inhaber:in", m.HolderName, Ink);
                        MetaRow(meta, "Ticket-Nr.", m.TicketRef, Ink);
                    });
                });
            });
        }).GeneratePdf();

    private static void MetaRow(ColumnDescriptor col, string label, string value, string valueColor) =>
        col.Item().BorderTop(1).BorderColor("#EEF0F2").PaddingVertical(1.5f).Row(row =>
        {
            row.ConstantItem(50).Text(label).FontColor(Muted).FontSize(7.5f);
            row.RelativeItem().AlignRight().Text(value).Bold().FontSize(7.5f).FontColor(valueColor);
        });
}
