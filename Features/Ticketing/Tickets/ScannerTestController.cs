using Microsoft.AspNetCore.Mvc;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.Tickets;

public sealed class ScannerTestController(
    ITicketTokens tokens, IQrCodeRenderer qr, IPublicBaseUrl publicUrl) : Controller
{
    private static readonly TicketType[] ExampleTypes =
        [TicketType.EventTicket, TicketType.SeasonSingle, TicketType.SeasonPass, TicketType.MemberCard];

    [HttpGet("/scanner-test")]
    public IActionResult Index()
    {
        var baseUrl = publicUrl.Resolve(Request);
        var cards = ExampleTypes.Select(type =>
        {
            var token = tokens.Create(type, Guid.Empty, 0);
            var svg = qr.RenderSvg($"{baseUrl}/ticket/{token}", 8);
            return ExampleCard(type, svg);
        }).ToList();
        return View("~/Views/ScannerTest.cshtml", new ScannerTestViewModel(cards));
    }

    private static TicketCardModel ExampleCard(TicketType type, string qrMarkup) => type switch
    {
        TicketType.EventTicket => new TicketCardModel(
            TicketDisplay.Kicker(type), TicketDisplay.TypeLabel(type),
            ScopeName: "Red Ants vs. UHC Beispielgegner", DateText: "15.11.2026, 18:00 Uhr",
            CategoryLabel: "Erwachsen", HolderName: null, Serial: "BEISPIEL",
            QrMarkup: qrMarkup, VenueName: "Sporthalle, Winterthur"),
        TicketType.SeasonSingle => new TicketCardModel(
            TicketDisplay.Kicker(type), TicketDisplay.TypeLabel(type),
            ScopeName: "Saison 2026/27", DateText: null,
            CategoryLabel: "Erwachsen", HolderName: null, Serial: "BEISPIEL", QrMarkup: qrMarkup),
        TicketType.SeasonPass => new TicketCardModel(
            TicketDisplay.Kicker(type), TicketDisplay.TypeLabel(type),
            ScopeName: "Saison 2026/27", DateText: "13.09.2026 – 22.03.2027",
            CategoryLabel: "Erwachsen", HolderName: null, Serial: "BEISPIEL", QrMarkup: qrMarkup),
        TicketType.MemberCard => new TicketCardModel(
            TicketDisplay.Kicker(type), TicketDisplay.TypeLabel(type),
            ScopeName: "Saison 2026/27", DateText: null,
            CategoryLabel: "Aktivmitglied", HolderName: "Erika Muster", Serial: "BEISPIEL", QrMarkup: qrMarkup),
        _ => new TicketCardModel(
            TicketDisplay.Kicker(type), TicketDisplay.TypeLabel(type),
            ScopeName: "Beispiel", DateText: null, CategoryLabel: null, HolderName: null,
            Serial: "BEISPIEL", QrMarkup: qrMarkup)
    };
}

public sealed record ScannerTestViewModel(IReadOnlyList<TicketCardModel> Cards);
