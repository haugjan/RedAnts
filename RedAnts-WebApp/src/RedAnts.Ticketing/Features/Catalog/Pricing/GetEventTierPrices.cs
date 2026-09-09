namespace RedAnts.Ticketing.Features.Catalog.Pricing;

public sealed record EventTierPrice(int TierId, string Name, bool Offered, decimal Price, int? Quota, DateOnly? AvailableUntil);

public static class GetEventTierPrices
{
    public sealed record Query(int EventId);

    public sealed class Handler(IEventsForAdminReader reader)
    {
        public Task<IReadOnlyList<EventTierPrice>> HandleAsync(Query query) => reader.GetTierPricesAsync(query.EventId);
    }
}
