using RedAnts.Show.Features.Halls;

namespace RedAnts.Show.Features.Games;

public sealed record GameSchedule(IReadOnlyList<UpcomingGame> Played, IReadOnlyList<UpcomingGame> Next);

public static class GetGameSchedule
{
    public const int DefaultDaysBack = 42;

    public sealed record Query(string HallId, int DaysBack = DefaultDaysBack);

    public sealed class Handler(IHallRepository halls, ISwissUnihockeyGames swissUnihockey)
    {
        public async Task<GameSchedule> HandleAsync(Query query)
        {
            var hall = await halls.GetByIdAsync(query.HallId)
                ?? throw new DomainException($"Halle „{query.HallId}“ nicht gefunden.");
            var games = await swissUnihockey.UpcomingAsync(hall.TeamId, hall.Season);
            return Split(games, SwissTime.Today.ToDateTime(TimeOnly.MinValue), query.DaysBack);
        }

        public static GameSchedule Split(IReadOnlyList<UpcomingGame> games, DateTime today, int daysBack)
        {
            var from = today.AddDays(-Math.Abs(daysBack));
            var played = games
                .Where(g => g.Date is { } date && date >= from && date < today)
                .OrderByDescending(g => g.Date)
                .ToList();
            var next = games
                .Where(g => g.Date is null || g.Date >= today)
                .OrderBy(g => g.Date)
                .ToList();
            return new GameSchedule(played, next);
        }
    }
}
