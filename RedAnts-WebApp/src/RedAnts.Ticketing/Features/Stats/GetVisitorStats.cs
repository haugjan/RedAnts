namespace RedAnts.Ticketing.Features.Stats;

public static class GetVisitorStats
{
    public sealed record Query(DateOnly From, DateOnly ToExclusive);

    public sealed class Handler(IVisitorStatsReader visitors)
    {
        public Task<VisitorOverview> HandleAsync(Query query) => visitors.GetAsync(query.From, query.ToExclusive);
    }
}
