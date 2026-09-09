using Microsoft.AspNetCore.Mvc;
using RedAnts.Ticketing.Features.Tickets;

namespace RedAnts.Ticketing.Features.Admission;

public sealed class ScannerTestController(GetScannerTestCards.Handler cards) : Controller
{
    [HttpGet("/scanner-test")]
    public async Task<IActionResult> Index() =>
        View("ScannerTest", new ScannerTestViewModel(await cards.HandleAsync(new GetScannerTestCards.Query())));
}

public sealed record ScannerTestViewModel(IReadOnlyList<TicketCardModel> Cards);
