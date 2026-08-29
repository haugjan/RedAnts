using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Tickets;
using Umbraco.Cms.Core;

namespace RedAnts.Features.Ticketing.Admin;

[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
public sealed class EventBundleExportController(
    IEventBundleTickets bundleTickets,
    ITicketTokens tokens,
    IPublicBaseUrl publicUrl) : Controller
{
    [HttpGet("/admin/event-tickets/tickets.csv")]
    public async Task<IActionResult> Export([FromQuery] string? ids)
    {
        var bundleIds = (ids ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var n) ? n : 0)
            .Where(n => n > 0)
            .Distinct()
            .ToList();

        var tickets = await bundleTickets.GetByBundlesAsync(bundleIds);
        var rows = tickets.Select(t => new TicketExportRow(
            t.Uuid.ToString("N")[..8].ToUpperInvariant(), t.Reference, t.Category.DisplayName(),
            t.Holder ?? CardHolder.Empty, null, publicUrl.TicketUrl(tokens.CreateShort(t.Uuid))));

        var name = bundleIds.Count == 1 ? $"spieltickets-bundle-{bundleIds[0]}.csv" : "spieltickets-bundles.csv";
        return File(TicketExportCsv.Build(rows), "text/csv; charset=utf-8", name);
    }
}
