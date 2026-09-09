namespace RedAnts.Ticketing.Features.FlexTickets;

public static class GetFlexBundles
{
    public sealed record Query(int SeasonId);

    public sealed class Handler(IFlexTicketBundles bundles)
    {
        public async Task<IReadOnlyList<FlexTicketBundleView>> HandleAsync(Query query) =>
            query.SeasonId <= 0 ? [] : await bundles.GetBySeasonAsync(query.SeasonId);
    }
}
