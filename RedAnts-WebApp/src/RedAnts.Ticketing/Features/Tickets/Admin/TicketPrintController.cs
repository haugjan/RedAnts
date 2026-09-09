using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.EventBundles;
using RedAnts.Ticketing.Features.FlexTickets;
using RedAnts.Ticketing.Features.MemberCards;
using RedAnts.Ticketing.Features.SeasonPasses;
using Umbraco.Cms.Core;

namespace RedAnts.Ticketing.Features.Tickets.Admin;

[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
public sealed class TicketPrintController(
    ITicketPrinter printer,
    ITicketPrintSettings settings,
    GetEventBundleTickets.Handler eventBundles,
    GetFlexBundlesForExport.Handler flexTickets,
    GetSeasonPassesForExport.Handler seasonPasses,
    GetMemberCardsForExport.Handler memberCards,
    IIssuedTicketReader issued) : Controller
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

        var items = await ResolveItemsAsync(type, bundleId, seasonId, reference, uuid);
        if (items.Count == 0) return NotFound("Keine Tickets zum Drucken gefunden.");

        using var buffer = new MemoryStream();
        await template.CopyToAsync(buffer);
        var pdf = await printer.BuildAsync(items, buffer.ToArray(), layout);

        await settings.SaveAsync(type, layout);

        var name = uuid is { } u ? u.ToString("N")[..8] : $"{type}-{bundleId ?? seasonId ?? 0}";
        return File(pdf, "application/pdf", $"tickets-{name}.pdf");
    }

    private async Task<IReadOnlyList<TicketPrintItem>> ResolveItemsAsync(
        TicketType type, int? bundleId, int? seasonId, string? reference, Guid? uuid)
    {
        if (uuid is { } single)
        {
            var ticket = await issued.FindAsync(single);
            return ticket is null ? [] : [new TicketPrintItem(single, ticket.HolderName ?? ticket.BuyerName)];
        }

        var reff = (reference ?? "").Trim();
        return type switch
        {
            TicketType.EventTicket when bundleId is { } bid =>
                (await eventBundles.HandleAsync(new GetEventBundleTickets.Query(bid)))
                    .Select(t => new TicketPrintItem(t.Uuid, t.Holder?.DisplayName)).ToList(),
            TicketType.SeasonSingle when bundleId is { } bid =>
                (await flexTickets.HandleAsync(new GetFlexBundlesForExport.Query([bid])))
                    .Select(t => new TicketPrintItem(t.Uuid, t.Holder?.DisplayName)).ToList(),
            TicketType.SeasonPass when seasonId is { } sid =>
                (await seasonPasses.HandleAsync(new GetSeasonPassesForExport.Query(sid, reff.Length == 0 ? [] : [reff])))
                    .Select(p => new TicketPrintItem(p.Uuid, p.Holder?.DisplayName ?? p.BuyerName)).ToList(),
            TicketType.MemberCard when seasonId is { } sid =>
                (await memberCards.HandleAsync(new GetMemberCardsForExport.Query(sid, reff.Length == 0 ? [] : [reff])))
                    .Select(c => new TicketPrintItem(c.Uuid, c.HolderName)).ToList(),
            _ => []
        };
    }

    private static double Positive(double value, double fallback) => value > 0 ? value : fallback;
}
