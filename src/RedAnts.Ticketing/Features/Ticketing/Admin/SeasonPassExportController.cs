using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedAnts.Features.Ticketing.Tickets;
using Umbraco.Cms.Core;

namespace RedAnts.Features.Ticketing.Admin;

[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
public sealed class SeasonPassExportController(
    ISeasonPassAdminReport report,
    ITicketTokens tokens,
    IPublicBaseUrl publicUrl) : Controller
{
    [HttpGet("/admin/season-passes/season/{seasonId:int}/passes.csv")]
    public async Task<IActionResult> Export(int seasonId, [FromQuery] string? bundles)
    {
        var selected = (bundles ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var passes = await report.GetBySeasonAsync(seasonId);
        if (selected.Length > 0)
            passes = passes.Where(p => selected.Contains(p.Reference ?? "", StringComparer.Ordinal)).ToList();

        var rows = passes.Select(p => new TicketExportRow(
            p.Uuid.ToString("N")[..8].ToUpperInvariant(), p.Reference, p.CategoryName,
            p.Holder ?? Domain.Ticketing.Sales.CardHolder.Empty, null,
            publicUrl.TicketUrl(tokens.CreateShort(p.Uuid))));

        var suffix = selected.Length == 1 ? SafeName(selected[0]) : $"{seasonId}";
        return File(TicketExportCsv.Build(rows), "text/csv; charset=utf-8", $"saisonkarten-{suffix}.csv");
    }

    private static string SafeName(string value) =>
        new(value.Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray());
}
