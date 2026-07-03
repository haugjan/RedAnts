namespace RedAnts.Domain.Ticketing;

/// <summary>An event (Anlass). Backed by an Umbraco Document Type. Pricing/sales are handled by the
/// separate <see cref="Sales"/> model, not on the catalog entity.</summary>
public sealed class Event
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public string? Text { get; private set; }
    public int SeasonId { get; private set; }
    public DateOnly Date { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public int VenueId { get; private set; }
    public EventStatus Status { get; private set; }
    public string? ImageUrl { get; private set; }
    public string? HomeTeamLogoUrl { get; private set; }
    public string? AwayTeamLogoUrl { get; private set; }
    /// <summary>Secret required to open this event when its status is Intern.</summary>
    public string? AccessSecret { get; private set; }

    private Event(int id, string name, string? text, int seasonId, DateOnly date, TimeOnly startTime,
        int venueId, EventStatus status, string? imageUrl, string? homeTeamLogoUrl,
        string? awayTeamLogoUrl, string? accessSecret)
    {
        Id = id;
        Name = name;
        Text = text;
        SeasonId = seasonId;
        Date = date;
        StartTime = startTime;
        VenueId = venueId;
        Status = status;
        ImageUrl = imageUrl;
        HomeTeamLogoUrl = homeTeamLogoUrl;
        AwayTeamLogoUrl = awayTeamLogoUrl;
        AccessSecret = accessSecret;
    }

    public static Event FromPersistence(int id, string name, string? text, int seasonId, DateOnly date,
        TimeOnly startTime, int venueId, EventStatus status, string? imageUrl,
        string? homeTeamLogoUrl, string? awayTeamLogoUrl, string? accessSecret) =>
        new(id, name ?? "", text, seasonId, date, startTime, venueId, status,
            imageUrl, homeTeamLogoUrl, awayTeamLogoUrl, accessSecret);
}
