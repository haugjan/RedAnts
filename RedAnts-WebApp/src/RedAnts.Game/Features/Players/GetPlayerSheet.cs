namespace RedAnts.Game.Features.Players;

public static class GetPlayerSheet
{
    public sealed record Query(int PlayerId);

    public sealed class Handler(IGamePlayerReader players)
    {
        public Task<PlayerSheetView?> HandleAsync(Query query) => players.GetSheetAsync(query.PlayerId);
    }
}
