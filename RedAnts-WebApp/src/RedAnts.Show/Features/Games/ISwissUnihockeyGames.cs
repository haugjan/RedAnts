using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Games;

public sealed record UpcomingGame(string Id, DateTime? Date, string Home, string Away, string Location, string Result, string League = "");

public interface ISwissUnihockeyGames
{
    Task<IReadOnlyList<UpcomingGame>> ClubHomeGamesAsync(int season, CancellationToken cancellation = default);
    Task<Game> LoadAsync(string gameId, CancellationToken cancellation = default);
}
