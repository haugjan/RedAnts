namespace RedAnts.Ticketing.Features.Catalog.Pricing;

public sealed record TierPassPrice(bool Offered, decimal Price, int? Quota, DateOnly? AvailableFrom, DateOnly? AvailableUntil)
{
    public static readonly TierPassPrice None = new(false, 0m, null, null, null);
}

public sealed record TierTicketPrice(bool Offered, decimal Price, int? Quota, DateOnly? AvailableUntil)
{
    public static readonly TierTicketPrice None = new(false, 0m, null, null);
}

public sealed record SeasonTierPromoPrice(int TierId, string Name, int Sold, TierPassPrice Pass, TierTicketPrice Ticket);

public sealed record SeasonTierPrice(
    int TierId,
    string Name,
    int? MinAge,
    int? MaxAge,
    int SortOrder,
    int Sold,
    TierPassPrice Pass,
    TierTicketPrice Ticket,
    SeasonTierPromoPrice? Promo);

public static class GetSeasonTierPrices
{
    public sealed record Query(int SeasonId);

    public sealed class Handler(ISeasonsForAdminReader reader)
    {
        public Task<IReadOnlyList<SeasonTierPrice>> HandleAsync(Query query) => reader.GetTierPricesAsync(query.SeasonId);
    }
}
