using RedAnts.Ticketing.Domain;

namespace RedAnts.Ticketing.Features.Catalog;

public interface IVenueReader
{
    Task<IReadOnlyList<Venue>> GetAllAsync();
    Task<Venue?> FindByIdAsync(int id);
}
