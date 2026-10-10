using RedAnts.Show.Features.Halls;

namespace RedAnts.Show.Features.Games;

public static class GetUpcomingGames
{
    public sealed record Query(string HallId);

    public sealed class Handler(IHallRepository halls, ISwissUnihockeyGames swissUnihockey)
    {
        public async Task<IReadOnlyList<UpcomingGame>> HandleAsync(Query query)
        {
            var hall = await halls.GetByIdAsync(query.HallId)
                ?? throw new DomainException($"Halle „{query.HallId}“ nicht gefunden.");
            var games = await swissUnihockey.UpcomingAsync(hall.TeamId, hall.Season);
            var yesterday = SwissTime.Today.ToDateTime(TimeOnly.MinValue).AddDays(-1);
            return games.Where(g => g.Date is null || g.Date >= yesterday).OrderBy(g => g.Date).ToList();
        }
    }
}
