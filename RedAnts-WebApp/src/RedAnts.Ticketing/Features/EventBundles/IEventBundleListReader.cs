namespace RedAnts.Ticketing.Features.EventBundles;

public interface IEventBundleListReader
{
    Task<IReadOnlyList<EventBundleRow>> GetByEventAsync(int eventId);
}
