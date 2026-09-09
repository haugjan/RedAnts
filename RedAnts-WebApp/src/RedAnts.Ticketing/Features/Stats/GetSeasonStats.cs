using RedAnts.Ticketing.Features.Catalog;

namespace RedAnts.Ticketing.Features.Stats;

public static class GetSeasonStats
{
    public sealed record Query(int SeasonId);

    public sealed class Handler(ISeasonVisitStatsReader stats, ISeasons seasons, IEvents events)
    {
        public async Task<SeasonVisitStats> HandleAsync(Query query)
        {
            var season = await seasons.FindByIdAsync(query.SeasonId);
            var eventIds = (await events.GetBySeasonAsync(query.SeasonId)).Select(e => e.Id).ToList();
            var seasonStart = season?.StartDate.ToDateTime(TimeOnly.MinValue) ?? SwissTime.Now;
            return await stats.GetAsync(query.SeasonId, eventIds, seasonStart);
        }
    }
}
