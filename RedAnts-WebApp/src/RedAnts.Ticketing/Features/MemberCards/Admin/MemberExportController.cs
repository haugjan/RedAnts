using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedAnts.Ticketing.Features.Tickets.Admin;
using Umbraco.Cms.Core;

namespace RedAnts.Ticketing.Features.MemberCards.Admin;

[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class MemberExportController(GetMemberCardsForExport.Handler cards) : Controller
{
    [HttpGet("/admin/members/season/{seasonId:int}/cards.csv")]
    public async Task<IActionResult> ExportCsv(int seasonId, [FromQuery] string? bundles)
    {
        var selected = (bundles ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (selected.Length == 0)
            return BadRequest("Bundle fehlt.");

        var rows = (await cards.HandleAsync(new GetMemberCardsForExport.Query(seasonId, selected)))
            .Select(c => new TicketExportRow(c.CardNo, c.Reference, c.CategoryLabel, c.Holder, c.Admissions, c.TicketUrl));

        var suffix = selected.Length == 1
            ? new string(selected[0].Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray())
            : "alle";
        return File(TicketExportCsv.Build(rows), "text/csv; charset=utf-8", $"mitglieder-{suffix}.csv");
    }
}
