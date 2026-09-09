using RedAnts.Ticketing.Features.Stats.Admin;

namespace RedAnts.Ticketing.Features.Stats;

public interface IAdmissionStatsReport
{
    Task<IReadOnlyDictionary<int, AdmissionCounts>> GetByEventsAsync(IReadOnlyCollection<int> eventIds);
}
