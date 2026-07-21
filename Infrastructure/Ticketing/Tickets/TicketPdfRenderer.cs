using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RedAnts.Features.Ticketing.Tickets;

namespace RedAnts.Infrastructure.Ticketing.Tickets;

public sealed class TicketPdfRenderer : ITicketPdf
{
    private const string Red = "#D02D38";
    private const string RedDk = "#B0242E";
    private const string Ink = "#14171A";
    private const string Muted = "#6B7178";

    public byte[] Render(TicketPdfModel m) =>
        Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(54, 85.6f, Unit.Millimetre);
                page.Margin(0);
                page.DefaultTextStyle(t => t.FontSize(8).FontColor(Ink));
                page.Content().Column(col =>
                {
                    col.Item().Background(Red).Padding(8).Column(head =>
                    {
                        head.Item().Row(row =>
                        {
                            row.RelativeItem().Text(m.Kicker.ToUpperInvariant()).FontColor(Colors.White).FontSize(6.5f);
                            row.AutoItem().AlignRight().Text("RED ANTS").FontColor(Colors.White).Bold().FontSize(6.5f);
                        });
                        head.Item().PaddingTop(1).Text(m.TypeLabel).FontColor(Colors.White).Bold().FontSize(14);
                    });

                    col.Item().PaddingTop(8).AlignCenter().Width(28, Unit.Millimetre).Image(m.QrPng);
                    col.Item().PaddingTop(2).AlignCenter().Text("Am Eingang scannen lassen").FontColor(Muted).FontSize(6.5f);

                    col.Item().PaddingHorizontal(12).PaddingVertical(6).LineHorizontal(1).LineColor("#D6DADE");

                    col.Item().PaddingHorizontal(14).Text(m.ScopeName).Bold().FontSize(10).FontColor(Ink);
                    col.Item().PaddingHorizontal(14).PaddingTop(2).Column(meta =>
                    {
                        if (m.DateText is not null) MetaRow(meta, "Datum", m.DateText, RedDk);
                        if (m.CategoryLabel is not null) MetaRow(meta, "Kategorie", m.CategoryLabel, Ink);
                        if (m.HolderName is not null) MetaRow(meta, "Name", m.HolderName, Ink);
                        MetaRow(meta, "Ticket-Nr.", m.TicketRef, Ink);
                    });
                });
            });
        }).GeneratePdf();

    private static void MetaRow(ColumnDescriptor col, string label, string value, string valueColor) =>
        col.Item().BorderTop(1).BorderColor("#EEF0F2").PaddingVertical(2).Row(row =>
        {
            row.RelativeItem().Text(label).FontColor(Muted).FontSize(7.5f);
            row.RelativeItem().AlignRight().Text(value).Bold().FontSize(7.5f).FontColor(valueColor);
        });
}
