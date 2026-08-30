using Microsoft.AspNetCore.Mvc;

namespace RedAnts.Features.Ticketing.Admin;

[ApiExplorerSettings(IgnoreApi = true)]
public sealed class FlexImportController : Controller
{
    [HttpGet("/admin/flex-tickets/example.csv")]
    public IActionResult SampleCsv() =>
        File(TicketImportCsv.SampleBytes(), "text/csv; charset=utf-8", "flextickets-vorlage.csv");
}
