using RedAnts.Ticketing.Features.Catalog.Admin;

namespace RedAnts.Ticketing.Features.Catalog;

public interface ISeasonLinkReader
{
    Task<IReadOnlyDictionary<int, SeasonLinks>> GetAllAsync();
}
