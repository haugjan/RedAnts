using RedAnts.Ticketing.Features.EventBundles.Admin;

namespace RedAnts.Ticketing.Features.EventBundles;

public interface IEventBundleTickets
{
    Task<IReadOnlyList<EventBundleTicket>> GetByBundleAsync(int bundleId);
    Task<IReadOnlyList<EventBundleTicket>> GetByBundlesAsync(IReadOnlyCollection<int> bundleIds);
}
