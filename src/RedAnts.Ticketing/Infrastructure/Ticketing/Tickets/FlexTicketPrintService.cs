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
    private const double QuietZoneMm = 2;

    public async Task<byte[]?> BuildAsync(int bundleId, byte[] templatePdf, FlexPrintLayout layout)
    {
        var tickets = await bundleTickets.GetByBundlesAsync([bundleId]);
        if (tickets.Count == 0) return null;

        using var templateStream = new MemoryStream(templatePdf);
        var template = XPdfForm.FromStream(templateStream);
        var black = new XSolidBrush(XColor.FromCmyk(0, 0, 0, 1));
        var paper = new XSolidBrush(XColor.FromCmyk(0, 0, 0, 0));

        using var document = new PdfDocument();
        document.Options.ColorMode = PdfColorMode.Cmyk;

        foreach (var ticket in tickets)
        {
            var page = document.AddPage();
            page.Width = XUnit.FromMillimeter(layout.PageWidthMm);
            page.Height = XUnit.FromMillimeter(layout.PageHeightMm);

            using var gfx = XGraphics.FromPdfPage(page);
            gfx.DrawImage(template, 0, 0, page.Width.Point, page.Height.Point);

            DrawQr(gfx, black, paper, layout, publicUrl.TicketUrl(tokens.CreateShort(ticket.Uuid)));

            if (layout.ShowCode)
                DrawCode(gfx, black, layout, ticket.Uuid.ToString("N")[..8].ToUpperInvariant());
        }

        using var output = new MemoryStream();
        document.Save(output, false);
        return output.ToArray();
    }

    private const int QrBuiltInQuietModules = 4;

    private static void DrawQr(XGraphics gfx, XBrush brush, XBrush paper, FlexPrintLayout layout, string content)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
        var matrix = data.ModuleMatrix;
        var total = matrix.Count;
        if (total <= QrBuiltInQuietModules * 2) return;

        var size = Mm(layout.QrSizeMm);
        var originX = Mm(layout.QrXMm);
        var originY = Mm(layout.QrYMm);
        var quiet = Mm(QuietZoneMm);

        var qz = QrBuiltInQuietModules;
        var dataCount = total - qz * 2;
        var module = size / dataCount;

        gfx.DrawRectangle(paper, originX - quiet, originY - quiet, size + 2 * quiet, size + 2 * quiet);

        for (var y = qz; y < total - qz; y++)
        {
            var row = matrix[y];
            for (var x = qz; x < total - qz; x++)
                if (row[x])
                    gfx.DrawRectangle(brush,
                        originX + (x - qz) * module,
                        originY + (y - qz) * module,
                        module, module);
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
