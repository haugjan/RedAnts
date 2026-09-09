using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Admin;

public sealed record FlexBundleTicket(Guid Uuid, int SeasonId, string Reference,
    TicketCategory Category = TicketCategory.Adult, CardHolder? Holder = null);

public interface IFlexBundleTickets
{
    Task<IReadOnlyList<FlexBundleTicket>> GetByBundlesAsync(IReadOnlyCollection<int> bundleIds);
}
