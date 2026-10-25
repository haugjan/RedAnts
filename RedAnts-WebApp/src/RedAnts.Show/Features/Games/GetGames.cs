using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Games;

public sealed record GameRow(string Id, string Title, DateTime? Date, string Home, string Away, GameSource Source, DateTimeOffset LoadedAt, int Spectators);

public static class GetGames
{
    public sealed record Query;

    public sealed class Handler(IGameReader reader)
    {
        public Task<IReadOnlyList<GameRow>> HandleAsync(Query query) => reader.ListAsync();
    }
}
