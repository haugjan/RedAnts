using RedAnts.Game.Domain;
using RedAnts.Game.Features.Shared;

namespace RedAnts.Game.Features.Players;

public interface IGamePlayerReader
{
    Task<IReadOnlyList<PlayerCardView>> GetMarketAsync(PlayerPosition? position, string? search, int take);

    Task<IReadOnlyList<PlayerCardView>> GetByIdsAsync(IReadOnlyCollection<int> playerIds);

    Task<PlayerSheetView?> GetSheetAsync(int playerId);
}
