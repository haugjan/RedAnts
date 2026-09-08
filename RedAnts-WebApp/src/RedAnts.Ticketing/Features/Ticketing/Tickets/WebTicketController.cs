using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RedAnts.Domain;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Core;

namespace RedAnts.Features.Ticketing.Tickets;

public sealed class WebTicketController(
    ITicketTokens tokens,
    IQrCodeRenderer qr,
    IIssuedTicketReader tickets,
    ITicketCustomNames customNames,
    IMyTicketsReader myTickets,
    IEvents events,
    ISeasons seasons,
    IVenues venues,
    IContentUrls contentUrls,
    IPublicBaseUrl publicUrl,
    ITicketPdf pdf,
    ILogger<WebTicketController> logger) : Controller
{
    [HttpGet("/ticket/{token}")]
    public async Task<IActionResult> Show(string token)
    {
        var data = await ResolveAsync(token);
        if (data is null)
            return View("~/Views/WebTicket.cshtml", WebTicketViewModel.Invalid());

        var issued = await tickets.FindAsync(data.Uuid);
        var (scopeName, dateText, venueName, homeLogo, awayLogo) = await ResolveContextAsync(data);

        var svg = qr.RenderSvg(QrUrl(data.Uuid));
        var holderDefault = issued?.HolderName ?? issued?.BuyerName;
        var displayName = FirstNonEmpty(issued?.CustomName, holderDefault);

        var next = (await BuildUpcomingAsync(1)).FirstOrDefault();
        var related = await BuildRelatedAsync(data.Uuid);

        var model = new WebTicketViewModel(
            Found: issued is not null,
            Valid: issued is { Status: TicketStatus.Valid },
            Kicker: TicketDisplay.Kicker(data.Type),
            TypeLabel: DisplayTitle(data.Type, issued),
            ScopeName: scopeName,
            DateText: data.Type == TicketType.EventTicket ? dateText : null,
            CategoryLabel: CategoryLabel(issued),
            HolderName: displayName,
            TicketRef: TicketRef(data.Uuid),
            QrSvg: svg,
            HomeLogo: homeLogo,
            AwayLogo: awayLogo,
            TypeKey: TypeKey(data.Type, issued?.MemberCategory),
            Token: token,
            VenueName: venueName,
            Admissions: issued?.Admissions ?? 1,
            CustomName: issued?.CustomName,
            HolderDefault: holderDefault,
            NextMatch: next,
            RelatedTickets: related);

        return View("~/Views/WebTicket.cshtml", model);
    }

    [HttpGet("/ticket/{token}/events")]
    public async Task<IActionResult> Events(string token)
    {
        var data = await ResolveAsync(token);
        if (data is null)
            return View("~/Views/WebTicketEvents.cshtml", TicketEventsViewModel.Invalid());

        var issued = await tickets.FindAsync(data.Uuid);
        var holder = FirstNonEmpty(issued?.CustomName, issued?.HolderName ?? issued?.BuyerName);
        var matches = await BuildUpcomingAsync(20);

        return View("~/Views/WebTicketEvents.cshtml", new TicketEventsViewModel(
            Found: true,
            Token: token,
            HolderName: holder,
            TypeLabel: DisplayTitle(data.Type, issued),
            Matches: matches));
    }

    [HttpPost("/ticket/{token}/name")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetName(string token, string? customName)
    {
        var data = await ResolveAsync(token);
        if (data is null) return NotFound();
        var issued = await tickets.FindAsync(data.Uuid);
        if (issued is not { Status: TicketStatus.Valid }) return NotFound();

        await customNames.SetAsync(data.Type, data.Uuid, customName);
        return RedirectToAction(nameof(Show), new { token });
    }

    [HttpGet("/ticket/{token}/manifest.webmanifest")]
    public async Task<IActionResult> Manifest(string token)
    {
        var data = await ResolveAsync(token);
        if (data is null) return NotFound();

        var manifest = new
        {
            name = "Red Ants Ticket",
            short_name = "Red Ants",
            description = "Dein Red Ants Online-Ticket.",
            start_url = $"/ticket/{token}",
            scope = "/ticket/",
            display = "standalone",
            orientation = "portrait",
            background_color = "#f4f4f5",
            theme_color = "#C8102E",
            lang = "de",
            icons = new object[]
            {
                new { src = "/favicons/android-chrome-192x192.png", sizes = "192x192", type = "image/png", purpose = "any" },
                new { src = "/favicons/android-chrome-384x384.png", sizes = "384x384", type = "image/png", purpose = "any" },
                new { src = "/favicons/android-chrome-192x192.png", sizes = "192x192", type = "image/png", purpose = "maskable" },
                new { src = "/favicons/android-chrome-384x384.png", sizes = "384x384", type = "image/png", purpose = "maskable" }
            }
        };

        var json = JsonSerializer.Serialize(manifest);
        return Content(json, "application/manifest+json");
    }

    [HttpGet("/ticket/{token}/qr.png")]
    public async Task<IActionResult> QrPng(string token)
    {
        var data = await ResolveAsync(token);
        if (data is null) return NotFound();
        var issued = await tickets.FindAsync(data.Uuid);
        if (issued is not { Status: TicketStatus.Valid }) return NotFound();
        return File(qr.RenderPng(QrUrl(data.Uuid), 8), "image/png");
    }

    [HttpGet("/ticket/{token}/pdf")]
    public async Task<IActionResult> Pdf(string token)
    {
        var data = await ResolveAsync(token);
        if (data is null) return NotFound();
        var issued = await tickets.FindAsync(data.Uuid);
        if (issued is not { Status: TicketStatus.Valid }) return NotFound();
        var (scopeName, dateText, venueName, _, _) = await ResolveContextAsync(data);

        byte[] bytes;
        try
        {
            bytes = pdf.Render(new TicketPdfModel(
                Kicker: TicketDisplay.Kicker(data.Type),
                TypeLabel: DisplayTitle(data.Type, issued),
                ScopeName: scopeName,
                DateText: dateText,
                CategoryLabel: CategoryLabel(issued),
                HolderName: FirstNonEmpty(issued?.CustomName, issued?.HolderName ?? issued?.BuyerName),
                TicketRef: TicketRef(data.Uuid),
                AccentHex: TypeAccentHex(data.Type),
                QrPng: qr.RenderPng(QrUrl(data.Uuid), 10),
                VenueName: venueName,
                Admissions: issued?.Admissions ?? 1));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ticket PDF render failed for {TicketRef} (type {Type}).", TicketRef(data.Uuid), data.Type);
            return StatusCode(StatusCodes.Status500InternalServerError,
                "Das Ticket-PDF konnte gerade nicht erzeugt werden. Bitte versuche es in einem Moment erneut.");
        }

        return File(bytes, "application/pdf", $"redants-ticket-{TicketRef(data.Uuid)}.pdf");
    }

    [Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
    [HttpGet("/ticket/for/{uuid:guid}")]
    public async Task<IActionResult> ForUuid(Guid uuid)
    {
        var issued = await tickets.FindAsync(uuid);
        if (issued is null) return NotFound();
        var token = tokens.CreateShort(issued.Uuid);
        return RedirectToAction(nameof(Show), new { token });
    }

    private async Task<TicketTokenData?> ResolveAsync(string token)
    {
        if (tokens.TryVerify(token, out var data)) return data;
        if (tokens.TryVerifyShort(token, out var code))
        {
            var issued = await tickets.FindByCodeAsync(code);
            if (issued is not null)
                return new TicketTokenData(issued.Type, issued.Uuid, issued.ScopeId, default);
        }
        return null;
    }

    private string QrUrl(Guid uuid) => publicUrl.TicketUrl(tokens.CreateShort(uuid));

    private async Task<IReadOnlyList<UpcomingMatch>> BuildUpcomingAsync(int limit)
    {
        var today = SwissTime.Today;
        var upcoming = (await events.GetPublicOpenAsync())
            .OrderBy(e => e.Date).ThenBy(e => e.StartTime)
            .Take(limit)
            .ToList();

        var result = new List<UpcomingMatch>(upcoming.Count);
        foreach (var ev in upcoming)
        {
            var venueName = ev.VenueId > 0 ? (await venues.FindByIdAsync(ev.VenueId))?.Name : null;
            var url = contentUrls.GetUrl(ev.Id);
            result.Add(new UpcomingMatch(
                Title: ev.Name,
                DateText: EventDateText(ev.Date, ev.StartTime, ev.TimeUnknown),
                VenueName: venueName,
                Url: string.IsNullOrEmpty(url) ? null : url,
                HomeLogo: ev.HomeTeamLogoUrl,
                AwayLogo: ev.AwayTeamLogoUrl,
                IsToday: ev.Date == today));
        }
        return result;
    }

    private async Task<IReadOnlyList<RelatedTicket>> BuildRelatedAsync(Guid current)
    {
        var email = await myTickets.FindBillingEmailAsync(current);
        if (string.IsNullOrWhiteSpace(email)) return [];

        var summaries = (await myTickets.GetByEmailAsync(email))
            .Where(s => s.Uuid != current && s.Status == TicketStatus.Valid)
            .Take(30)
            .ToList();

        var result = new List<RelatedTicket>(summaries.Count);
        foreach (var s in summaries)
        {
            var (scopeName, dateText) = await ResolveScopeAsync(s.Type, s.ScopeId);
            if (s.Type != TicketType.EventTicket) dateText = null;
            var issued = await tickets.FindAsync(s.Uuid);
            var name = FirstNonEmpty(issued?.CustomName, issued?.HolderName ?? issued?.BuyerName);
            result.Add(new RelatedTicket(
                Token: tokens.CreateShort(s.Uuid),
                Kicker: TicketDisplay.Kicker(s.Type),
                TypeLabel: TicketDisplay.TypeLabel(s.Type),
                ScopeName: scopeName,
                DateText: dateText,
                DisplayName: name,
                TypeKey: TypeKey(s.Type, issued?.MemberCategory)));
        }
        return result;
    }

    private async Task<(string ScopeName, string? DateText)> ResolveScopeAsync(TicketType type, int scopeId)
    {
        if (type == TicketType.EventTicket)
        {
            var ev = await events.FindByIdAsync(scopeId);
            return ev is null ? ("Anlass", null) : (ev.Name, EventDateText(ev.Date, ev.StartTime, ev.TimeUnknown));
        }
        var season = await seasons.FindByIdAsync(scopeId);
        return season is null
            ? ("Saison", null)
            : (season.Name, $"{season.StartDate:dd.MM.yyyy} – {season.EndDate:dd.MM.yyyy}");
    }

    private async Task<(string ScopeName, string? DateText, string? VenueName, string? HomeLogo, string? AwayLogo)> ResolveContextAsync(TicketTokenData data)
    {
        if (data.Type == TicketType.EventTicket)
        {
            var ev = await events.FindByIdAsync(data.ScopeId);
            if (ev is null) return ("Anlass", null, null, null, null);
            var dateText = EventDateText(ev.Date, ev.StartTime, ev.TimeUnknown);
            var venueName = ev.VenueId > 0 ? (await venues.FindByIdAsync(ev.VenueId))?.Name : null;
            return (ev.Name, dateText, venueName, ev.HomeTeamLogoUrl, ev.AwayTeamLogoUrl);
        }

        var season = await seasons.FindByIdAsync(data.ScopeId);
        return season is null
            ? ("Saison", null, null, null, null)
            : (season.Name, $"{season.StartDate:dd.MM.yyyy} – {season.EndDate:dd.MM.yyyy}", null, null, null);
    }

    private static string EventDateText(DateOnly date, TimeOnly start, bool timeUnknown) =>
        timeUnknown ? $"{date:dd.MM.yyyy}" : $"{date:dd.MM.yyyy}, {start:HH:mm} Uhr";

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

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
        TicketType.MemberCard => member is { } m && m.IsBlock4() ? "block4" : "member",
        TicketType.FreeEntry => "free",
        _ => "spiel"
    };
}

public sealed record UpcomingMatch(
    string Title,
    string DateText,
    string? VenueName,
    string? Url,
    string? HomeLogo,
    string? AwayLogo,
    bool IsToday);

public sealed record RelatedTicket(
    string Token,
    string Kicker,
    string TypeLabel,
    string ScopeName,
    string? DateText,
    string? DisplayName,
    string TypeKey);

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
    string? VenueName = null,
    int Admissions = 1,
    string? CustomName = null,
    string? HolderDefault = null,
    UpcomingMatch? NextMatch = null,
    IReadOnlyList<RelatedTicket>? RelatedTickets = null)
{
    public static WebTicketViewModel Invalid() =>
        new(false, false, "", "Ticket", "", null, null, null, "", "");
}

public sealed record TicketEventsViewModel(
    bool Found,
    string Token,
    string? HolderName,
    string TypeLabel,
    IReadOnlyList<UpcomingMatch> Matches)
{
    public static TicketEventsViewModel Invalid() =>
        new(false, "", null, "Ticket", []);
}
