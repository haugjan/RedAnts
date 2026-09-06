using RedAnts.Domain.Ticketing;

namespace RedAnts.Features.Ticketing.Ports;

public interface ISeasons
{
    Task<IReadOnlyList<Season>> GetAllAsync();
    Task<IReadOnlyList<Season>> GetPublicOpenAsync();
    Task<Season?> FindByIdAsync(int id);
}

public interface IVenues
{
    Task<IReadOnlyList<Venue>> GetAllAsync();
    Task<Venue?> FindByIdAsync(int id);
}

public interface IEvents
{
    Task<IReadOnlyList<Event>> GetAllAsync();
    Task<IReadOnlyList<Event>> GetPublicOpenAsync();
    Task<IReadOnlyList<Event>> GetUpcomingForScanningAsync();
    Task<IReadOnlyList<Event>> GetBySeasonAsync(int seasonId);
    Task<Event?> FindByIdAsync(int id);
}
