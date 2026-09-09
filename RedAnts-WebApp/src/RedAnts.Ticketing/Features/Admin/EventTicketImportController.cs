using Microsoft.AspNetCore.Mvc;

namespace RedAnts.Ticketing.Features.Admin;

[ApiExplorerSettings(IgnoreApi = true)]
public sealed class EventTicketImportController : Controller
{
    [HttpGet("/admin/event-tickets/example.csv")]
    public IActionResult SampleCsv() =>
        File(TicketImportCsv.SampleBytes(), "text/csv; charset=utf-8", "spieltickets-vorlage.csv");
}
