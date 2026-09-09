using RedAnts.Ticketing.Domain;

namespace RedAnts.Ticketing.Features.Catalog;

public interface ISeasons
{
    Task<IReadOnlyList<Season>> GetAllAsync();
    Task<IReadOnlyList<Season>> GetPublicOpenAsync();
    Task<Season?> FindByIdAsync(int id);
}
