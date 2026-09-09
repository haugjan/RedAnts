using RedAnts.Ticketing.Domain;

namespace RedAnts.Ticketing.Features.Catalog;

public interface IEvents
{
    Task<IReadOnlyList<Event>> GetAllAsync();
    Task<IReadOnlyList<Event>> GetPublicOpenAsync();
    Task<IReadOnlyList<Event>> GetUpcomingForScanningAsync();
    Task<IReadOnlyList<Event>> GetBySeasonAsync(int seasonId);
    Task<Event?> FindByIdAsync(int id);
}
