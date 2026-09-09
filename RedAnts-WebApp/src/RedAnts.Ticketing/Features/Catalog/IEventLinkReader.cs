using RedAnts.Ticketing.Features.Catalog.Admin;

namespace RedAnts.Ticketing.Features.Catalog;

public interface IEventLinkReader
{
    Task<IReadOnlyDictionary<int, EventLinks>> GetBySeasonAsync(int seasonId);
}
