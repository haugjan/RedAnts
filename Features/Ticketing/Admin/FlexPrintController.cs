using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Core;

namespace RedAnts.Features.Ticketing.Admin;

[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
public sealed class FlexPrintController(IFlexTicketPrinter printer) : Controller
{
    [HttpPost("/admin/flex-tickets/print")]
    [RequestSizeLimit(64 * 1024 * 1024)]
    public async Task<IActionResult> Print(
        [FromForm] int bundleId,
        [FromForm] IFormFile? template,
        [FromForm] double pageW,
        [FromForm] double pageH,
        [FromForm] double qrX,
        [FromForm] double qrY,
        [FromForm] double qrSize,
        [FromForm] double fontPt,
        [FromForm] bool showCode)
    {
        if (bundleId <= 0) return BadRequest("Kein Bundle gewählt.");
        if (template is null || template.Length == 0) return BadRequest("Keine Vorlage hochgeladen.");

        using var buffer = new MemoryStream();
        await template.CopyToAsync(buffer);

        var layout = new FlexPrintLayout(
            Positive(pageW, 91), Positive(pageH, 61),
            Math.Max(0, qrX), Math.Max(0, qrY),
            Positive(qrSize, 25), Positive(fontPt, 8), showCode);

        var pdf = await printer.BuildAsync(bundleId, buffer.ToArray(), layout);
        if (pdf is null) return BadRequest("Das Bundle enthält keine Flextickets.");

        return File(pdf, "application/pdf",
            string.Create(CultureInfo.InvariantCulture, $"flextickets-bundle-{bundleId}.pdf"));
    }

    private static double Positive(double value, double fallback) => value > 0 ? value : fallback;
}
