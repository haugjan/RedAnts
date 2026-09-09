using RedAnts.Ticketing.Features.Catalog;

namespace RedAnts.Ticketing.Features.Stats;

public static class GetSalesStats
{
    public sealed record Query(int SeasonId);

    public sealed class Handler(ISalesStatsReader sales, IEvents events)
    {
        public async Task<SalesStats> HandleAsync(Query query)
        {
            var eventIds = (await events.GetBySeasonAsync(query.SeasonId)).Select(e => e.Id).ToList();
            return await sales.GetSeasonAsync(query.SeasonId, eventIds);
        }
    }
}
