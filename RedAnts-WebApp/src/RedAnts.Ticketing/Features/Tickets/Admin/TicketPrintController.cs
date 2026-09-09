using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedAnts.Ticketing.Domain.Sales;
using Umbraco.Cms.Core;

namespace RedAnts.Ticketing.Features.Tickets.Admin;

[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
public sealed class TicketPrintController(PrintTickets.Handler printing) : Controller
{
    [HttpPost("/admin/tickets/print")]
    [RequestSizeLimit(64 * 1024 * 1024)]
    public async Task<IActionResult> Print(
        [FromForm] int ticketType,
        [FromForm] IFormFile? template,
        [FromForm] double pageW,
        [FromForm] double pageH,
        [FromForm] double qrX,
        [FromForm] double qrY,
        [FromForm] double qrSize,
        [FromForm] double fontPt,
        [FromForm] bool showName,
        [FromForm] double nameX,
        [FromForm] double nameY,
        [FromForm] double nameFontPt,
        [FromForm] double nameMaxW,
        [FromForm] int nameAlign,
        [FromForm] int? bundleId,
        [FromForm] int? seasonId,
        [FromForm] string? reference,
        [FromForm] Guid? uuid)
    {
        if (template is null || template.Length == 0)
            return BadRequest("Es wurde keine Vorlage hochgeladen.");
        if (!Enum.IsDefined(typeof(TicketType), ticketType))
            return BadRequest("Unbekannter Tickettyp.");

        var type = (TicketType)ticketType;
        var layout = new TicketPrintLayout(
            Positive(pageW, 91), Positive(pageH, 61),
            Math.Max(0, qrX), Math.Max(0, qrY), Positive(qrSize, 25), Positive(fontPt, 8),
            showName, Math.Max(0, nameX), Math.Max(0, nameY), Positive(nameFontPt, 9), Positive(nameMaxW, 52),
            nameAlign is 1 or 2 ? nameAlign : 0);

        using var buffer = new MemoryStream();
        await template.CopyToAsync(buffer);
        var pdf = await printing.HandleAsync(new PrintTickets.Command(type, buffer.ToArray(), layout, bundleId, seasonId, reference, uuid));
        if (pdf is null) return NotFound("Keine Tickets zum Drucken gefunden.");

        var name = uuid is { } u ? u.ToString("N")[..8] : $"{type}-{bundleId ?? seasonId ?? 0}";
        return File(pdf, "application/pdf", $"tickets-{name}.pdf");
    }

    private static double Positive(double value, double fallback) => value > 0 ? value : fallback;
}
