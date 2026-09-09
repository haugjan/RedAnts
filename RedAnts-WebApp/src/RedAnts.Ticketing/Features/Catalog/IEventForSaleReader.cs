using RedAnts.Ticketing.Features.Catalog.Shop;

namespace RedAnts.Ticketing.Features.Catalog;

public interface IEventForSaleReader
{
    Task<EventForSale?> FindAsync(int eventId);
}
