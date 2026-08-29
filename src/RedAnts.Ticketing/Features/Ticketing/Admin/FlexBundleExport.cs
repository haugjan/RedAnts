using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Admin;

public sealed record FlexBundleTicket(Guid Uuid, int SeasonId, string Reference,
    TicketCategory Category = TicketCategory.Adult, CardHolder? Holder = null);

public interface IFlexBundleTickets
{
    Task<IReadOnlyList<FlexBundleTicket>> GetByBundlesAsync(IReadOnlyCollection<int> bundleIds);
}
