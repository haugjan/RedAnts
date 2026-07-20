using Microsoft.AspNetCore.Mvc;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.Tickets;

public sealed class ScannerTestController(
    ITicketTokens tokens, IQrCodeRenderer qr, IPublicBaseUrl publicUrl) : Controller
{
    [HttpGet("/scanner-test")]
    public IActionResult Index()
    {
        var token = tokens.Create(TicketType.EventTicket, Guid.Empty, 0);
        var url = $"{publicUrl.Resolve(Request)}/ticket/{token}";
        var svg = qr.RenderSvg(url, 8);
        return View("~/Views/ScannerTest.cshtml", new ScannerTestViewModel(svg));
    }
}

public sealed record ScannerTestViewModel(string QrSvg);
