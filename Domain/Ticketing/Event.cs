namespace RedAnts.Domain.Ticketing;

/// <summary>An event (Anlass) for which single tickets can be sold. Backed by an Umbraco Document Type.</summary>
public sealed class Event
{
    private readonly List<EventPrice> _prices;

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
    /// <summary>Secret required to open/buy this event when its status is Intern.</summary>
    public string? AccessSecret { get; private set; }
    public IReadOnlyList<EventPrice> Prices => _prices;
    /// <summary>Resolved sales prices from the content Block List (category + effective price + contingent).</summary>
    public IReadOnlyList<TicketPrice> SalesPrices { get; private set; }

    private Event(int id, string name, string? text, int seasonId, DateOnly date, TimeOnly startTime,
        int venueId, EventStatus status, string? imageUrl, string? homeTeamLogoUrl,
        string? awayTeamLogoUrl, string? accessSecret, IEnumerable<EventPrice> prices,
        IReadOnlyList<TicketPrice>? salesPrices)
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
        _prices = NormalizePrices(prices);
        SalesPrices = salesPrices ?? [];
    }

    public static Event FromPersistence(int id, string name, string? text, int seasonId, DateOnly date,
        TimeOnly startTime, int venueId, EventStatus status, string? imageUrl,
        string? homeTeamLogoUrl, string? awayTeamLogoUrl, string? accessSecret, IEnumerable<EventPrice> prices,
        IReadOnlyList<TicketPrice>? salesPrices = null) =>
        new(id, name ?? "", text, seasonId, date, startTime, venueId, status,
            imageUrl, homeTeamLogoUrl, awayTeamLogoUrl, accessSecret, prices, salesPrices);

    /// <summary>Price for a category, or null if no price is defined for it.</summary>
    public decimal? PriceFor(PriceCategory category) =>
        _prices.FirstOrDefault(p => p.Category == category)?.Amount;

    private static List<EventPrice> NormalizePrices(IEnumerable<EventPrice> prices) =>
        (prices ?? [])
            .GroupBy(p => p.Category)
            .Select(g => g.Last())
            .ToList();
}
