using Microsoft.AspNetCore.Mvc;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.Tickets;

/// <summary>Public web ticket: renders a ticket and its QR code. The QR encodes the absolute URL of
/// this page (which carries the signed token), so a generic phone camera opens the ticket while the
/// door scanner (S5) parses the token out of the path and calls <see cref="ITicketTokens.TryVerify"/>.
/// Fixed MVC routes, auto-discovered via UseWebsiteEndpoints() (same as CartController).</summary>
public sealed class WebTicketController(
    ITicketTokens tokens,
    IQrCodeRenderer qr,
    IIssuedTicketReader tickets,
    IEvents events,
    ISeasons seasons) : Controller
{
    /// <summary>The ticket page for a signed token.</summary>
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
                dateText = $"{ev.Date:dd.MM.yyyy}, {ev.StartTime:HH:mm} Uhr";
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
            TypeLabel: TypeLabel(data.Type),
            ScopeName: scopeName,
            DateText: dateText,
            CategoryLabel: issued is null ? null : (issued.Category?.DisplayName() ?? issued.MemberCategory?.DisplayName()),
            HolderName: issued?.HolderName,
            TicketRef: data.Uuid.ToString("N")[..8].ToUpperInvariant(),
            QrSvg: svg,
            HomeLogo: homeLogo,
            AwayLogo: awayLogo,
            MemberLogoUrl: issued?.MemberCategory is { } mc ? MemberLogo(mc) : null);

        return View("~/Views/WebTicket.cshtml", model);
    }

    /// <summary>Convenience: mint the token for an issued ticket (by its Uuid) and redirect to its
    /// ticket page. Lets a ticket URL be produced without other slices being wired up yet.</summary>
    [HttpGet("/ticket/for/{uuid:guid}")]
    public async Task<IActionResult> ForUuid(Guid uuid)
    {
        var issued = await tickets.FindAsync(uuid);
        if (issued is null) return NotFound();
        var token = tokens.Create(issued.Type, issued.Uuid, issued.ScopeId);
        return RedirectToAction(nameof(Show), new { token });
    }

    private static string TypeLabel(TicketType type) => type switch
    {
        TicketType.EventTicket => "Spielticket",
        TicketType.SeasonSingle => "Flexticket",
        TicketType.SeasonPass => "Saisonkarte",
        TicketType.MemberCard => "Mitgliederkarte",
        TicketType.FreeEntry => "Freier Eintritt",
        _ => "Ticket"
    };

    /// <summary>Logo shown on a member card, chosen by its member category.</summary>
    private static string MemberLogo(MemberCategory category) => category switch
    {
        MemberCategory.RedAnts => "/img/members/red-ants.webp",
        MemberCategory.Block4 => "/img/members/block4.webp",
        _ => "/img/members/red-ants.webp"
    };
}

/// <summary>View data for <c>Views/WebTicket.cshtml</c>.</summary>
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
    string? MemberLogoUrl = null)
{
    public static WebTicketViewModel Invalid() =>
        new(false, false, "Ticket", "", null, null, null, "", "");
}
