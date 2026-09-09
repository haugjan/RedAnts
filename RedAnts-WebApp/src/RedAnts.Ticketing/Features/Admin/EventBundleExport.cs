using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Admin;

public sealed record EventBundleTicket(Guid Uuid, int EventId, string Reference,
    TicketCategory Category = TicketCategory.Adult, CardHolder? Holder = null);

public interface IEventBundleTickets
{
    Task<IReadOnlyList<EventBundleTicket>> GetByBundleAsync(int bundleId);
    Task<IReadOnlyList<EventBundleTicket>> GetByBundlesAsync(IReadOnlyCollection<int> bundleIds);
}
