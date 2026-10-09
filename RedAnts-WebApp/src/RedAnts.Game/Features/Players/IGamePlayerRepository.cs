using RedAnts.Game.Domain;

namespace RedAnts.Game.Features.Players;

public interface IGamePlayerRepository
{
    Task<GamePlayer?> GetByExternalIdAsync(int externalId);

    Task<int> ReplaceAllAsync(IReadOnlyList<GamePlayer> players);

    Task<int> CountAsync();
}
