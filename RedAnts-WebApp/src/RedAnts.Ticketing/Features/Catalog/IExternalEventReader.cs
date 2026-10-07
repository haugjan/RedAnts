using RedAnts.Ticketing.Features.Catalog.Shop;

namespace RedAnts.Ticketing.Features.Catalog;

public interface IExternalEventReader
{
    Task<IReadOnlyList<ExternalEvent>> GetUpcomingAsync();
}
