using RedAnts.Ticketing.Features.Catalog.Shop;

namespace RedAnts.Ticketing.Features.Catalog;

public interface INextEventReader
{
    Task<NextEvent?> FindAsync();
}
