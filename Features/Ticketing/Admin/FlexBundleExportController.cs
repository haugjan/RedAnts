using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Tickets;
using Umbraco.Cms.Core;

namespace RedAnts.Features.Ticketing.Admin;

/// <summary>Exports a whole Flexticket bundle. Two downloads: a two-column CSV (short code + ticket
/// link) and a print-ready Word document (RTF, opens natively in Word) with one page per ticket showing
/// its short code and an embedded QR code. Behind the backoffice auth scheme, like the ticketing admin
/// dashboard. Tokens/QR are minted per ticket via S3's <see cref="ITicketTokens"/> / <see cref="IQrCodeRenderer"/>.</summary>
[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
public sealed class FlexBundleExportController(
    IFlexBundleTickets bundleTickets,
    ITicketTokens tokens,
    IQrCodeRenderer qr) : Controller
{
    [HttpGet("/admin/flextickets/bundle/{bundleId:int}/tickets.csv")]
    public async Task<IActionResult> Export(int bundleId)
    {
        var tickets = await bundleTickets.GetByBundleAsync(bundleId);

        var sb = new StringBuilder();
        sb.Append("Kurzkennung;Link\r\n");
        foreach (var t in tickets)
        {
            var link = $"{Request.Scheme}://{Request.Host}/ticket/{tokens.Create(TicketType.SeasonSingle, t.Uuid, t.SeasonId)}";
            sb.Append(ShortCode(t.Uuid)).Append(';').Append(link).Append("\r\n");
        }

        // Prepend a UTF-8 BOM so Excel opens the file with the right encoding.
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"flextickets-bundle-{bundleId}.csv");
    }

    /// <summary>A Word mail-merge document (RTF): one page per Flexticket with its short code and QR code.
    /// RTF keeps this dependency-free (Word opens it natively); each QR is embedded as a PNG picture.</summary>
    [HttpGet("/admin/flextickets/bundle/{bundleId:int}/serienbrief.rtf")]
    public async Task<IActionResult> Serienbrief(int bundleId)
    {
        var tickets = await bundleTickets.GetByBundleAsync(bundleId);

        var sb = new StringBuilder();
        sb.Append(@"{\rtf1\ansi\ansicpg1252\deff0{\fonttbl{\f0\fswiss Arial;}}\f0 ");
        var first = true;
        foreach (var t in tickets)
        {
            if (!first) sb.Append(@"\page ");
            first = false;

            var link = $"{Request.Scheme}://{Request.Host}/ticket/{tokens.Create(TicketType.SeasonSingle, t.Uuid, t.SeasonId)}";

            sb.Append(@"\qc\sb360\b\fs40 Red Ants Winterthur\b0\par ");
            sb.Append(@"\fs28 Flexticket\par\par ");
            sb.Append(QrPicture(link)).Append(@"\par\par ");
            sb.Append(@"\fs24 Kurzkennung: \b ").Append(ShortCode(t.Uuid)).Append(@"\b0\par ");
            sb.Append(@"Scanne diesen Code am Eingang.\par ");
        }
        sb.Append('}');

        // RTF is pure ASCII (control words + hex image data + ASCII text).
        return File(Encoding.ASCII.GetBytes(sb.ToString()), "application/rtf", $"flextickets-serienbrief-{bundleId}.rtf");
    }

    private static string ShortCode(Guid uuid) => uuid.ToString("N")[..8].ToUpperInvariant();

    /// <summary>An RTF <c>\pict</c> group holding the QR as an embedded PNG, sized ~4.5 cm square.</summary>
    private string QrPicture(string content)
    {
        var dataUri = qr.RenderPngDataUri(content, 10);
        var png = Convert.FromBase64String(dataUri[(dataUri.IndexOf(',') + 1)..]);

        // Intrinsic pixel size from the PNG IHDR (bytes 16..24), converted to himetric (0.01 mm) at 96 dpi.
        int pw = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
        int ph = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
        int picw = (int)(pw * 2540.0 / 96);
        int pich = (int)(ph * 2540.0 / 96);
        const int goal = 2551; // ~4.5 cm in twips

        return $@"{{\pict\pngblip\picw{picw}\pich{pich}\picwgoal{goal}\pichgoal{goal} {Convert.ToHexString(png)}}}";
    }
}
