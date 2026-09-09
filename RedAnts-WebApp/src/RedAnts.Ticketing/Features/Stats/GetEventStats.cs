using RedAnts.Ticketing.Features.Catalog;

namespace RedAnts.Ticketing.Features.Stats;

public static class GetEventStats
{
    public sealed record Query(int EventId);

    public sealed class Handler(IEventVisitStatsReader stats, IEventReader events)
    {
        public async Task<EventVisitStats?> HandleAsync(Query query)
        {
            var eventItem = query.EventId > 0 ? await events.FindByIdAsync(query.EventId) : null;
            return eventItem is null ? null : await stats.GetAsync(query.EventId, eventItem.Date.ToDateTime(eventItem.StartTime));
        }
    }
}
