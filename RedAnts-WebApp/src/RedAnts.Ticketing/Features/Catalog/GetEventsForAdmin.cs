namespace RedAnts.Ticketing.Features.Catalog;

public sealed record EventForAdmin(int Id, string Name, DateOnly Date, TimeOnly StartTime);

public static class GetEventsForAdmin
{
    public sealed record Query(int SeasonId);

    public sealed class Handler(IEvents events)
    {
        public async Task<IReadOnlyList<EventForAdmin>> HandleAsync(Query query) =>
            query.SeasonId <= 0
                ? []
                : (await events.GetBySeasonAsync(query.SeasonId))
                    .OrderBy(e => e.Date).ThenBy(e => e.StartTime)
                    .Select(e => new EventForAdmin(e.Id, e.Name, e.Date, e.StartTime))
                    .ToList();
    }
}
