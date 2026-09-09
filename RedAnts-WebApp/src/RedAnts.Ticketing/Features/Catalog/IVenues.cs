using RedAnts.Ticketing.Domain;

namespace RedAnts.Ticketing.Features.Catalog;

public interface IVenues
{
    Task<IReadOnlyList<Venue>> GetAllAsync();
    Task<Venue?> FindByIdAsync(int id);
}
