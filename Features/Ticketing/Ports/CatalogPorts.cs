using RedAnts.Domain.Ticketing;

namespace RedAnts.Features.Ticketing.Ports;

// Catalog entities (Season/Venue/Event) are native Umbraco Document Types edited in the Content tree.
// These ports are read-only; CRUD happens in the Umbraco backoffice, not through code.

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
    /// <summary>Events with status Open whose date is today or in the future (public overview).</summary>
    Task<IReadOnlyList<Event>> GetPublicOpenAsync();
    Task<IReadOnlyList<Event>> GetBySeasonAsync(int seasonId);
    Task<Event?> FindByIdAsync(int id);
}
