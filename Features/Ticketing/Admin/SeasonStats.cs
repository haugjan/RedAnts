namespace RedAnts.Features.Ticketing.Admin;

public sealed record SeasonStats(int PassesSold, int TicketsSold, int Admissions);

public interface ISeasonStatsReader
{
    Task<SeasonStats> GetAsync(int seasonId, IReadOnlyList<int> eventIds);
}
