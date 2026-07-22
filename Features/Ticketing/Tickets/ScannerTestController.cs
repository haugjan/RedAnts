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
        var baseUrl = publicUrl.Resolve(Request);
        var items = Enum.GetValues<TicketType>()
            .Select(type =>
            {
                var token = tokens.Create(type, Guid.Empty, 0);
                var svg = qr.RenderSvg($"{baseUrl}/ticket/{token}", 8);
                return new ScannerTestItem(
                    TicketDisplay.TypeLabel(type), TicketDisplay.Kicker(type), TicketDisplay.AccentHex(type), svg);
            })
            .ToList();
        return View("~/Views/ScannerTest.cshtml", new ScannerTestViewModel(items));
    }
}

public sealed record ScannerTestItem(string Label, string Kicker, string AccentHex, string QrSvg);

public sealed record ScannerTestViewModel(IReadOnlyList<ScannerTestItem> Items);
