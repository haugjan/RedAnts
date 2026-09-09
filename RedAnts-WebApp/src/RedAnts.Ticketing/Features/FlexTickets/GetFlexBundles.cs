namespace RedAnts.Ticketing.Features.FlexTickets;

public static class GetFlexBundles
{
    public sealed record Query(int SeasonId);

    public sealed class Handler(IFlexBundleListReader reader)
    {
        public Task<IReadOnlyList<FlexBundleRow>> HandleAsync(Query query) => reader.GetBySeasonAsync(query.SeasonId);
    }
}
