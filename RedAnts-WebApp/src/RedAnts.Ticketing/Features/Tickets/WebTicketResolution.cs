using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Catalog;

namespace RedAnts.Ticketing.Features.Tickets;

public sealed class WebTicketResolution(
    ITicketTokens tokens,
    IIssuedTicketReader tickets,
    IEventReader events,
    ISeasonReader seasons,
    IVenueReader venues,
    IContentUrls contentUrls)
{
    public sealed record Resolved(TicketTokenData Data, IssuedTicket? Issued);

    public sealed record Context(string ScopeName, string? DateText, string? VenueName, string? HomeLogo, string? AwayLogo);

    public async Task<Resolved?> ResolveAsync(string token)
    {
        if (tokens.TryVerify(token, out var data)) return new Resolved(data, await tickets.FindAsync(data.Uuid));
        if (tokens.TryVerifyShort(token, out var code))
        {
            var issued = await tickets.FindByCodeAsync(code);
            if (issued is not null)
                return new Resolved(new TicketTokenData(issued.Type, issued.Uuid, issued.ScopeId, default), issued);
        }
        return null;
    }

    public async Task<Context> ContextAsync(TicketType type, int scopeId)
    {
        if (type == TicketType.EventTicket)
        {
            var ev = await events.FindByIdAsync(scopeId);
            if (ev is null) return new Context("Anlass", null, null, null, null);
            var venueName = ev.VenueId > 0 ? (await venues.FindByIdAsync(ev.VenueId))?.Name : null;
            return new Context(ev.Name, EventDateText(ev.Date, ev.StartTime, ev.TimeUnknown), venueName, ev.HomeTeamLogoUrl, ev.AwayTeamLogoUrl);
        }

        var season = await seasons.FindByIdAsync(scopeId);
        return season is null
            ? new Context("Saison", null, null, null, null)
            : new Context(season.Name, $"{season.StartDate:dd.MM.yyyy} – {season.EndDate:dd.MM.yyyy}", null, null, null);
    }

    public async Task<IReadOnlyList<UpcomingMatch>> UpcomingAsync(int limit)
    {
        var today = SwissTime.Today;
        var upcoming = (await events.GetPublicOpenAsync())
            .OrderBy(e => e.Date).ThenBy(e => e.StartTime)
            .Take(limit)
            .ToList();

        var venueNames = new Dictionary<int, string?>();
        var result = new List<UpcomingMatch>(upcoming.Count);
        foreach (var ev in upcoming)
        {
            if (ev.VenueId > 0 && !venueNames.ContainsKey(ev.VenueId))
                venueNames[ev.VenueId] = (await venues.FindByIdAsync(ev.VenueId))?.Name;
            var url = contentUrls.GetUrl(ev.Id);
            result.Add(new UpcomingMatch(
                Title: ev.Name,
                DateText: EventDateText(ev.Date, ev.StartTime, ev.TimeUnknown),
                VenueName: ev.VenueId > 0 ? venueNames[ev.VenueId] : null,
                Url: string.IsNullOrEmpty(url) ? null : url,
                HomeLogo: ev.HomeTeamLogoUrl,
                AwayLogo: ev.AwayTeamLogoUrl,
                IsToday: ev.Date == today));
        }
        return result;
    }

    public string QrUrl(Guid uuid, IPublicBaseUrl publicUrl) => publicUrl.TicketUrl(tokens.CreateShort(uuid));

    public static string EventDateText(DateOnly date, TimeOnly start, bool timeUnknown) =>
        timeUnknown ? $"{date:dd.MM.yyyy}" : $"{date:dd.MM.yyyy}, {start:HH:mm} Uhr";

    public static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    public static string? CategoryLabel(IssuedTicket? issued) =>
        issued is null ? null : (issued.CategoryName ?? issued.Category?.DisplayName() ?? issued.MemberCategory?.DisplayName());

    public static string TicketRef(Guid uuid) => uuid.ToString("N")[..8].ToUpperInvariant();

    public static string DisplayTitle(TicketType type, IssuedTicket? issued) =>
        type == TicketType.MemberCard && issued?.MemberCategory is { } category
            ? category.DisplayName()
            : TicketDisplay.TypeLabel(type);

    public static string TypeAccentHex(TicketType type) => type switch
    {
        TicketType.EventTicket => "#C8102E",
        TicketType.SeasonSingle => "#E4720F",
        TicketType.SeasonPass => "#1F5FBF",
        TicketType.MemberCard => "#1A7F37",
        TicketType.FreeEntry => "#6B4EA0",
        _ => "#C8102E"
    };

    public static string TypeKey(TicketType type, MemberCategory? member) => type switch
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
