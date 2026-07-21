using QuestPDF.Fluent;
using QuestPDF.Helpers;
using RedAnts.Features.Ticketing.Tickets;

namespace RedAnts.Infrastructure.Ticketing.Tickets;

public sealed class TicketPdfRenderer : ITicketPdf
{
    public byte[] Render(TicketPdfModel m) =>
        Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A6);
                page.Margin(0);
                page.DefaultTextStyle(t => t.FontSize(11).FontColor("#101010"));
                page.Content().Column(col =>
                {
                    col.Item().Background(m.AccentHex).Padding(18).Column(head =>
                    {
                        head.Item().Text(m.TypeLabel).FontColor(Colors.White).Bold().FontSize(18);
                        head.Item().PaddingTop(2).Text(m.ScopeName).FontColor(Colors.White).FontSize(12);
                    });

                    col.Item().PaddingTop(18).AlignCenter().Width(190).Image(m.QrPng);
                    col.Item().PaddingTop(4).AlignCenter().Text("Am Eingang scannen lassen").FontColor("#888888").FontSize(9);

                    col.Item().PaddingHorizontal(20).PaddingTop(12).Column(meta =>
                    {
                        if (m.DateText is not null) MetaRow(meta, "Datum", m.DateText);
                        if (m.CategoryLabel is not null) MetaRow(meta, "Kategorie", m.CategoryLabel);
                        if (m.HolderName is not null) MetaRow(meta, "Name", m.HolderName);
                        MetaRow(meta, "Ticket-Nr.", m.TicketRef);
                    });
                });
            });
        }).GeneratePdf();

    private static void MetaRow(ColumnDescriptor col, string label, string value) =>
        col.Item().BorderTop(1).BorderColor("#F0F0F0").PaddingVertical(5).Row(row =>
        {
            row.RelativeItem().Text(label).FontColor("#666666").FontSize(10);
            row.RelativeItem().AlignRight().Text(value).Bold().FontSize(10);
        });
}
