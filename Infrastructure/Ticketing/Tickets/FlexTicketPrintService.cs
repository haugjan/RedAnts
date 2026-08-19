using PdfSharp.Drawing;
using PdfSharp.Pdf;
using QRCoder;
using RedAnts.Features.Ticketing.Admin;
using RedAnts.Features.Ticketing.Ports;
using RedAnts.Features.Ticketing.Tickets;

namespace RedAnts.Infrastructure.Ticketing.Tickets;

public sealed class FlexTicketPrintService(
    IFlexBundleTickets bundleTickets,
    ITicketTokens tokens,
    IPublicBaseUrl publicUrl) : IFlexTicketPrinter
{
    public async Task<byte[]?> BuildAsync(int bundleId, byte[] templatePdf, FlexPrintLayout layout)
    {
        var tickets = await bundleTickets.GetByBundlesAsync([bundleId]);
        if (tickets.Count == 0) return null;

        using var templateStream = new MemoryStream(templatePdf);
        var template = XPdfForm.FromStream(templateStream);
        var black = new XSolidBrush(XColor.FromCmyk(0, 0, 0, 1));

        using var document = new PdfDocument();
        document.Options.ColorMode = PdfColorMode.Cmyk;

        foreach (var ticket in tickets)
        {
            var page = document.AddPage();
            page.Width = XUnit.FromMillimeter(layout.PageWidthMm);
            page.Height = XUnit.FromMillimeter(layout.PageHeightMm);

            using var gfx = XGraphics.FromPdfPage(page);
            gfx.DrawImage(template, 0, 0, page.Width.Point, page.Height.Point);

            DrawQr(gfx, black, layout, publicUrl.TicketUrl(tokens.CreateShort(ticket.Uuid)));

            if (layout.ShowCode)
                DrawCode(gfx, black, layout, ticket.Uuid.ToString("N")[..8].ToUpperInvariant());
        }

        using var output = new MemoryStream();
        document.Save(output, false);
        return output.ToArray();
    }

    private static void DrawQr(XGraphics gfx, XBrush brush, FlexPrintLayout layout, string content)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
        var matrix = data.ModuleMatrix;
        var count = matrix.Count;
        if (count == 0) return;

        var size = Mm(layout.QrSizeMm);
        var originX = Mm(layout.QrXMm);
        var originY = Mm(layout.QrYMm);
        var module = size / count;

        for (var y = 0; y < count; y++)
        {
            var row = matrix[y];
            for (var x = 0; x < count; x++)
                if (row[x])
                    gfx.DrawRectangle(brush, originX + x * module, originY + y * module, module, module);
        }
    }

    private static void DrawCode(XGraphics gfx, XBrush brush, FlexPrintLayout layout, string code)
    {
        var font = new XFont(FlexPrintFontResolver.FamilyName, layout.CodeFontPt);
        var size = Mm(layout.QrSizeMm);
        var rect = new XRect(Mm(layout.QrXMm), Mm(layout.QrYMm) + size + layout.CodeFontPt * 0.4,
            size, layout.CodeFontPt * 1.6);
        gfx.DrawString(code, font, brush, rect, XStringFormats.TopCenter);
    }

    private static double Mm(double mm) => XUnit.FromMillimeter(mm).Point;
}
