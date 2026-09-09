namespace RedAnts.Ticketing.Features.Catalog.Shop;

public static class GetEventForSale
{
    public sealed record Query(int EventId);

    public sealed class Handler(IEventForSaleReader reader)
    {
        public Task<EventForSale?> HandleAsync(Query query) => reader.FindAsync(query.EventId);
    }
}
