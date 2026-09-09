namespace RedAnts.Ticketing.Features.EventBundles;

public interface IEventBundleTicketsReader
{
    Task<IReadOnlyList<EventBundleTicketRow>> GetByBundleAsync(int bundleId);
    Task<IReadOnlyList<EventBundleTicketRow>> GetByBundlesAsync(IReadOnlyCollection<int> bundleIds);
}
