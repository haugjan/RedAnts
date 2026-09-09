using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Tickets.Admin;
using Umbraco.Cms.Core;

namespace RedAnts.Ticketing.Features.SeasonPasses.Admin;

[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
public sealed class SeasonPassExportController(GetSeasonPassesForExport.Handler passes) : Controller
{
    [HttpGet("/admin/season-passes/season/{seasonId:int}/passes.csv")]
    public async Task<IActionResult> Export(int seasonId, [FromQuery] string? bundles)
    {
        var selected = (bundles ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var rows = (await passes.HandleAsync(new GetSeasonPassesForExport.Query(seasonId, selected)))
            .Select(p => new TicketExportRow(p.CardNo, p.Reference, p.CategoryName, p.Holder ?? CardHolder.Empty, null, p.TicketUrl));

        var suffix = selected.Length == 1 ? SafeName(selected[0]) : $"{seasonId}";
        return File(TicketExportCsv.Build(rows), "text/csv; charset=utf-8", $"saisonkarten-{suffix}.csv");
    }

    private static string SafeName(string value) =>
        new(value.Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray());
}
