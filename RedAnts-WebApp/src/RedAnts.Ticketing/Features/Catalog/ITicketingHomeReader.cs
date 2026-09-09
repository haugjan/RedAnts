using RedAnts.Ticketing.Features.Catalog.Shop;

namespace RedAnts.Ticketing.Features.Catalog;

public interface ITicketingHomeReader
{
    Task<TicketingHome> GetAsync();
}
