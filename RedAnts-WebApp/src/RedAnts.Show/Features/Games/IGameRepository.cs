using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Games;

public interface IGameRepository
{
    Task<Game?> GetByIdAsync(string id);
    Task SaveAsync(Game game);
    Task DeleteAsync(string id);
}
