using RedAnts.Ticketing.Features.Stats.Admin;

namespace RedAnts.Ticketing.Features.Stats;

public interface IEventVisitStatsReport
{
    Task<EventVisitStats> GetAsync(int eventId, DateTime kickoffSwiss);
}
