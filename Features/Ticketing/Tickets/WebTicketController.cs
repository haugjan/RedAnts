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
    ISeasons seasons,
    IVenues venues,
    IPublicBaseUrl publicUrl,
    ITicketPdf pdf) : Controller
{
    [HttpGet("/ticket/{token}")]
    public async Task<IActionResult> Show(string token)
    {
        if (!tokens.TryVerify(token, out var data))
            return View("~/Views/WebTicket.cshtml", WebTicketViewModel.Invalid());

        var issued = await tickets.FindAsync(data.Uuid);
        var (scopeName, dateText, venueName, homeLogo, awayLogo) = await ResolveContextAsync(data);

        var absoluteUrl = $"{publicUrl.Resolve()}/ticket/{token}";
        var svg = qr.RenderSvg(absoluteUrl);

        var model = new WebTicketViewModel(
            Found: issued is not null,
            Valid: issued is { Status: TicketStatus.Valid },
            Kicker: TicketDisplay.Kicker(data.Type),
            TypeLabel: DisplayTitle(data.Type, issued),
            ScopeName: scopeName,
            DateText: dateText,
            CategoryLabel: CategoryLabel(issued),
            HolderName: issued?.HolderName,
            TicketRef: TicketRef(data.Uuid),
            QrSvg: svg,
            HomeLogo: homeLogo,
            AwayLogo: awayLogo,
            TypeKey: TypeKey(data.Type, issued?.MemberCategory),
            Token: token,
            VenueName: venueName);

        return View("~/Views/WebTicket.cshtml", model);
    }

    [HttpGet("/ticket/{token}/qr.png")]
    public IActionResult QrPng(string token)
    {
        if (!tokens.TryVerify(token, out _)) return NotFound();
        var url = $"{publicUrl.Resolve()}/ticket/{token}";
        return File(qr.RenderPng(url, 8), "image/png");
    }

    [HttpGet("/ticket/{token}/pdf")]
    public async Task<IActionResult> Pdf(string token)
    {
        if (!tokens.TryVerify(token, out var data)) return NotFound();
        var issued = await tickets.FindAsync(data.Uuid);
        var (scopeName, dateText, venueName, _, _) = await ResolveContextAsync(data);
        var absoluteUrl = $"{publicUrl.Resolve()}/ticket/{token}";

        var bytes = pdf.Render(new TicketPdfModel(
            Kicker: TicketDisplay.Kicker(data.Type),
            TypeLabel: DisplayTitle(data.Type, issued),
            ScopeName: scopeName,
            DateText: dateText,
            CategoryLabel: CategoryLabel(issued),
            HolderName: issued?.HolderName,
            TicketRef: TicketRef(data.Uuid),
            AccentHex: TypeAccentHex(data.Type),
            QrPng: qr.RenderPng(absoluteUrl, 10),
            VenueName: venueName));

        return File(bytes, "application/pdf", $"redants-ticket-{TicketRef(data.Uuid)}.pdf");
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

    private async Task<(string ScopeName, string? DateText, string? VenueName, string? HomeLogo, string? AwayLogo)> ResolveContextAsync(TicketTokenData data)
    {
        if (data.Type == TicketType.EventTicket)
        {
            var ev = await events.FindByIdAsync(data.ScopeId);
            if (ev is null) return ("Anlass", null, null, null, null);
            var dateText = ev.TimeUnknown ? $"{ev.Date:dd.MM.yyyy}" : $"{ev.Date:dd.MM.yyyy}, {ev.StartTime:HH:mm} Uhr";
            var venueName = ev.VenueId > 0 ? (await venues.FindByIdAsync(ev.VenueId))?.Name : null;
            return (ev.Name, dateText, venueName, ev.HomeTeamLogoUrl, ev.AwayTeamLogoUrl);
        }

        var season = await seasons.FindByIdAsync(data.ScopeId);
        return season is null
            ? ("Saison", null, null, null, null)
            : (season.Name, $"{season.StartDate:dd.MM.yyyy} – {season.EndDate:dd.MM.yyyy}", null, null, null);
    }

    private static string? CategoryLabel(IssuedTicket? issued) =>
        issued is null ? null : (issued.CategoryName ?? issued.Category?.DisplayName() ?? issued.MemberCategory?.DisplayName());

    private static string TicketRef(Guid uuid) => uuid.ToString("N")[..8].ToUpperInvariant();

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

    private static string TypeAccentHex(TicketType type) => type switch
    {
        TicketType.EventTicket => "#C8102E",
        TicketType.SeasonSingle => "#E4720F",
        TicketType.SeasonPass => "#1F5FBF",
        TicketType.MemberCard => "#1A7F37",
        TicketType.FreeEntry => "#6B4EA0",
        _ => "#C8102E"
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
    string Kicker,
    string TypeLabel,
    string ScopeName,
    string? DateText,
    string? CategoryLabel,
    string? HolderName,
    string TicketRef,
    string QrSvg,
    string? HomeLogo = null,
    string? AwayLogo = null,
    string TypeKey = "spiel",
    string Token = "",
    string? VenueName = null)
{
    public static WebTicketViewModel Invalid() =>
        new(false, false, "", "Ticket", "", null, null, null, "", "");
}
