using Microsoft.AspNetCore.Mvc;
using RedAnts.Ticketing.Features.Tickets.Admin;

namespace RedAnts.Ticketing.Features.SeasonPasses.Admin;

[ApiExplorerSettings(IgnoreApi = true)]
public sealed class SeasonPassImportController : Controller
{
    [HttpGet("/admin/season-passes/example.csv")]
    public IActionResult SampleCsv() =>
        File(TicketImportCsv.SampleBytes(), "text/csv; charset=utf-8", "saisonkarten-vorlage.csv");
}
