using RedAnts.Ticketing.Features.FlexTickets.Admin;

namespace RedAnts.Ticketing.Features.FlexTickets;

public interface IFlexBundleTickets
{
    Task<IReadOnlyList<FlexBundleTicket>> GetByBundlesAsync(IReadOnlyCollection<int> bundleIds);
}
