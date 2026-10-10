using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Halls;

public interface IHallRepository
{
    Task<Hall?> GetByIdAsync(string id);
    Task SaveAsync(Hall hall);
    Task DeleteAsync(string id);
}
