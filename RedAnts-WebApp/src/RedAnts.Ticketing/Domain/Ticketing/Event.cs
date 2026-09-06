namespace RedAnts.Domain.Ticketing;

public sealed class Event
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public string? Text { get; private set; }
    public int SeasonId { get; private set; }
    public DateOnly Date { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public bool TimeUnknown { get; private set; }
    public int VenueId { get; private set; }
    public EventStatus Status { get; private set; }
    public string? ImageUrl { get; private set; }
    public string? HomeTeamLogoUrl { get; private set; }
    public string? AwayTeamLogoUrl { get; private set; }
    public string? AccessSecret { get; private set; }

    private Event(int id, string name, string? text, int seasonId, DateOnly date, TimeOnly startTime,
        bool timeUnknown, int venueId, EventStatus status, string? imageUrl, string? homeTeamLogoUrl,
        string? awayTeamLogoUrl, string? accessSecret)
    {
        Id = id;
        Name = name;
        Text = text;
        SeasonId = seasonId;
        Date = date;
        StartTime = startTime;
        TimeUnknown = timeUnknown;
        VenueId = venueId;
        Status = status;
        ImageUrl = imageUrl;
        HomeTeamLogoUrl = homeTeamLogoUrl;
        AwayTeamLogoUrl = awayTeamLogoUrl;
        AccessSecret = accessSecret;
    }

    public static Event FromPersistence(int id, string name, string? text, int seasonId, DateOnly date,
        TimeOnly startTime, bool timeUnknown, int venueId, EventStatus status, string? imageUrl,
        string? homeTeamLogoUrl, string? awayTeamLogoUrl, string? accessSecret) =>
        new(id, name ?? "", text, seasonId, date, startTime, timeUnknown, venueId, status,
            imageUrl, homeTeamLogoUrl, awayTeamLogoUrl, accessSecret);
}
