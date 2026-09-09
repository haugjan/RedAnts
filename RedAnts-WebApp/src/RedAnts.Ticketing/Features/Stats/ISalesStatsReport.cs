using RedAnts.Ticketing.Features.Stats.Admin;

namespace RedAnts.Ticketing.Features.Stats;

public interface ISalesStatsReport
{
    Task<SalesStats> GetSeasonAsync(int seasonId, IReadOnlyCollection<int> eventIds);
}
