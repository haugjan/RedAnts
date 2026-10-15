using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Games;

public sealed record UpcomingGame(string Id, DateTime? Date, string Home, string Away, string Location, string Result);

public interface ISwissUnihockeyGames
{
    Task<IReadOnlyList<UpcomingGame>> UpcomingAsync(int teamId, int season, CancellationToken cancellation = default);
    Task<Game> LoadAsync(string gameId, CancellationToken cancellation = default);
}
