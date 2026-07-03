namespace RedAnts.Domain.Ticketing;

/// <summary>A season (Saison) grouping events and season tickets. Backed by an Umbraco Document Type.</summary>
public sealed class Season
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public SeasonStatus Status { get; private set; }
    public string? ImageUrl { get; private set; }
    /// <summary>Secret required to open/buy this season when its status is Intern.</summary>
    public string? AccessSecret { get; private set; }
    /// <summary>Resolved sales prices from the content Block List (category + effective price + contingent).</summary>
    public IReadOnlyList<TicketPrice> SalesPrices { get; private set; }

    private Season(int id, string name, DateOnly startDate, DateOnly endDate, SeasonStatus status,
        string? imageUrl, string? accessSecret, IReadOnlyList<TicketPrice>? salesPrices)
    {
        Id = id;
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        Status = status;
        ImageUrl = imageUrl;
        AccessSecret = accessSecret;
        SalesPrices = salesPrices ?? [];
    }

    public static Season FromPersistence(int id, string name, DateOnly startDate, DateOnly endDate,
        SeasonStatus status, string? imageUrl = null, string? accessSecret = null,
        IReadOnlyList<TicketPrice>? salesPrices = null) =>
        new(id, name ?? "", startDate, endDate, status, imageUrl, accessSecret, salesPrices);
}
