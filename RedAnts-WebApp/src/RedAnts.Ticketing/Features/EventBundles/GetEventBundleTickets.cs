namespace RedAnts.Ticketing.Features.EventBundles;

public static class GetEventBundleTickets
{
    public sealed record Query(int BundleId);

    public sealed class Handler(IEventBundleTicketsReader reader)
    {
        public Task<IReadOnlyList<EventBundleTicketRow>> HandleAsync(Query query) => reader.GetByBundleAsync(query.BundleId);
    }
}
