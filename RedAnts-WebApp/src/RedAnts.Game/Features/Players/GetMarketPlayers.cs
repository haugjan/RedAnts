using RedAnts.Game.Domain;
using RedAnts.Game.Features.Shared;

namespace RedAnts.Game.Features.Players;

public static class GetMarketPlayers
{
    public const int DefaultTake = 60;

    public sealed record Query(PlayerPosition? Position = null, string? Search = null, int Take = DefaultTake);

    public sealed class Handler(IGamePlayerReader players)
    {
        public Task<IReadOnlyList<PlayerCardView>> HandleAsync(Query query) =>
            players.GetMarketAsync(query.Position, query.Search, Math.Clamp(query.Take, 1, 400));
    }
}
