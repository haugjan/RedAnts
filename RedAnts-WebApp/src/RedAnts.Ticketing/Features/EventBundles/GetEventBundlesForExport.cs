namespace RedAnts.Ticketing.Features.EventBundles;

public static class GetEventBundlesForExport
{
    public sealed record Query(IReadOnlyCollection<int> BundleIds);

    public sealed class Handler(IEventBundleTicketsReader reader)
    {
        public Task<IReadOnlyList<EventBundleTicketRow>> HandleAsync(Query query) => reader.GetByBundlesAsync(query.BundleIds);
    }
}
