namespace RedAnts.Ticketing.Features.FlexTickets;

public static class GetFlexBundlesForExport
{
    public sealed record Query(IReadOnlyCollection<int> BundleIds);

    public sealed class Handler(IFlexBundleTicketsReader reader)
    {
        public Task<IReadOnlyList<FlexTicketRow>> HandleAsync(Query query) => reader.GetByBundlesAsync(query.BundleIds);
    }
}
