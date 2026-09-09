namespace RedAnts.Ticketing.Features.Stats;

public interface IEventVisitStatsReader
{
    Task<EventVisitStats> GetAsync(int eventId, DateTime kickoffSwiss);
}
