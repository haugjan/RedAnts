using RedAnts.Ticketing.Features.Stats.Admin;

namespace RedAnts.Ticketing.Features.Stats;

public interface ISeasonVisitStatsReport
{
    Task<SeasonVisitStats> GetAsync(int seasonId, IReadOnlyCollection<int> eventIds, DateTime seasonStartSwiss);
}
