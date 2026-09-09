using Microsoft.AspNetCore.Mvc;
using RedAnts.Ticketing.Features.Tickets.Admin;

namespace RedAnts.Ticketing.Features.FlexTickets.Admin;

[ApiExplorerSettings(IgnoreApi = true)]
public sealed class FlexImportController : Controller
{
    [HttpGet("/admin/flex-tickets/example.csv")]
    public IActionResult SampleCsv() =>
        File(TicketImportCsv.SampleBytes(), "text/csv; charset=utf-8", "flextickets-vorlage.csv");
}
