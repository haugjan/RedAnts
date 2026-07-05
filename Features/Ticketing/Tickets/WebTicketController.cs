using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Core;

namespace RedAnts.Features.Ticketing.Tickets;

public sealed class WebTicketController(
    ITicketTokens tokens,
    IQrCodeRenderer qr,
    IIssuedTicketReader tickets,
    IEvents events,
    ISeasons seasons) : Controller
{
    [HttpGet("/ticket/{token}")]
    public async Task<IActionResult> Show(string token)
    {
        if (!tokens.TryVerify(token, out var data))
            return View("~/Views/WebTicket.cshtml", WebTicketViewModel.Invalid());

        var issued = await tickets.FindAsync(data.Uuid);

        string scopeName;
        string? dateText = null;
        string? homeLogo = null;
        string? awayLogo = null;
        if (data.Type == TicketType.EventTicket)
        {
            var ev = await events.FindByIdAsync(data.ScopeId);
            scopeName = ev?.Name ?? "Anlass";
            if (ev is not null)
            {
                dateText = ev.TimeUnknown ? $"{ev.Date:dd.MM.yyyy}" : $"{ev.Date:dd.MM.yyyy}, {ev.StartTime:HH:mm} Uhr";
                homeLogo = ev.HomeTeamLogoUrl;
                awayLogo = ev.AwayTeamLogoUrl;
            }
        }
        else
        {
            var season = await seasons.FindByIdAsync(data.ScopeId);
            scopeName = season?.Name ?? "Saison";
            if (season is not null)
                dateText = $"{season.StartDate:dd.MM.yyyy} – {season.EndDate:dd.MM.yyyy}";
        }

        var absoluteUrl = $"{Request.Scheme}://{Request.Host}/ticket/{token}";
        var svg = qr.RenderSvg(absoluteUrl);

        var model = new WebTicketViewModel(
            Found: issued is not null,
            Valid: issued is { Status: TicketStatus.Valid },
            TypeLabel: DisplayTitle(data.Type, issued),
            ScopeName: scopeName,
            DateText: dateText,
            CategoryLabel: issued is null ? null : (issued.Category?.DisplayName() ?? issued.MemberCategory?.DisplayName()),
            HolderName: issued?.HolderName,
            TicketRef: data.Uuid.ToString("N")[..8].ToUpperInvariant(),
            QrSvg: svg,
            HomeLogo: homeLogo,
            AwayLogo: awayLogo,
            TypeKey: TypeKey(data.Type, issued?.MemberCategory));

        return View("~/Views/WebTicket.cshtml", model);
    }

    [HttpGet("/ticket/{token}/qr.png")]
    public IActionResult QrPng(string token)
    {
        if (!tokens.TryVerify(token, out _)) return NotFound();
        var url = $"{Request.Scheme}://{Request.Host}/ticket/{token}";
        return File(qr.RenderPng(url, 8), "image/png");
    }

    [Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
    [HttpGet("/ticket/for/{uuid:guid}")]
    public async Task<IActionResult> ForUuid(Guid uuid)
    {
        var issued = await tickets.FindAsync(uuid);
        if (issued is null) return NotFound();
        var token = tokens.Create(issued.Type, issued.Uuid, issued.ScopeId);
        return RedirectToAction(nameof(Show), new { token });
    }

    private static string DisplayTitle(TicketType type, IssuedTicket? issued) =>
        type == TicketType.MemberCard && issued?.MemberCategory is { } category
            ? category.DisplayName()
            : TypeLabel(type);

    private static string TypeLabel(TicketType type) => type switch
    {
        TicketType.EventTicket => "Spielticket",
        TicketType.SeasonSingle => "Flexticket",
        TicketType.SeasonPass => "Saisonkarte",
        TicketType.MemberCard => "Mitgliederkarte",
        TicketType.FreeEntry => "Freier Eintritt",
        _ => "Ticket"
    };

    private static string TypeKey(TicketType type, MemberCategory? member) => type switch
    {
        TicketType.EventTicket => "spiel",
        TicketType.SeasonSingle => "flex",
        TicketType.SeasonPass => "saison",
        TicketType.MemberCard => member == MemberCategory.Block4 ? "block4" : "member",
        TicketType.FreeEntry => "free",
        _ => "spiel"
    };

}

public sealed record WebTicketViewModel(
    bool Found,
    bool Valid,
    string TypeLabel,
    string ScopeName,
    string? DateText,
    string? CategoryLabel,
    string? HolderName,
    string TicketRef,
    string QrSvg,
    string? HomeLogo = null,
    string? AwayLogo = null,
    string TypeKey = "spiel")
{
    public static WebTicketViewModel Invalid() =>
        new(false, false, "Ticket", "", null, null, null, "", "");
}
