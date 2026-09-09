namespace RedAnts.Ticketing.Features.Stats;

public interface ISalesStatsReader
{
    Task<SalesStats> GetSeasonAsync(int seasonId, IReadOnlyCollection<int> eventIds);
}
