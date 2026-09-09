namespace RedAnts.Ticketing.Features.Stats;

public interface ISeasonVisitStatsReader
{
    Task<SeasonVisitStats> GetAsync(int seasonId, IReadOnlyCollection<int> eventIds, DateTime seasonStartSwiss);
}
