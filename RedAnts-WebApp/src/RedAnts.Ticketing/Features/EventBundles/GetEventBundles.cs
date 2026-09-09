namespace RedAnts.Ticketing.Features.EventBundles;

public static class GetEventBundles
{
    public sealed record Query(int EventId);

    public sealed class Handler(IEventBundleListReader reader)
    {
        public Task<IReadOnlyList<EventBundleRow>> HandleAsync(Query query) => reader.GetByEventAsync(query.EventId);
    }
}
