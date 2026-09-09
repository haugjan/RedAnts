using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.EventBundles.Admin;

public sealed record EventBundleTicket(Guid Uuid, int EventId, string Reference,
    TicketCategory Category = TicketCategory.Adult, CardHolder? Holder = null);
