using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Tickets.Admin;
using Umbraco.Cms.Core;

namespace RedAnts.Ticketing.Features.FlexTickets.Admin;

[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
public sealed class FlexBundleExportController(GetFlexBundlesForExport.Handler tickets) : Controller
{
    [HttpGet("/admin/flex-tickets/bundles.csv")]
    public async Task<IActionResult> Export([FromQuery] string? ids)
    {
        var bundleIds = (ids ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var n) ? n : 0)
            .Where(n => n > 0)
            .Distinct()
            .ToList();

        var rows = (await tickets.HandleAsync(new GetFlexBundlesForExport.Query(bundleIds)))
            .Select(t => new TicketExportRow(t.CardNo, t.BundleReference, t.CategoryLabel, t.Holder ?? CardHolder.Empty, null, t.TicketUrl));

        var name = bundleIds.Count == 1 ? $"flextickets-bundle-{bundleIds[0]}.csv" : "flextickets-bundles.csv";
        return File(TicketExportCsv.Build(rows), "text/csv; charset=utf-8", name);
    }
}
