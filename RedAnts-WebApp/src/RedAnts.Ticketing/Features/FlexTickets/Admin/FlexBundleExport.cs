using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.FlexTickets.Admin;

public sealed record FlexBundleTicket(Guid Uuid, int SeasonId, string Reference,
    TicketCategory Category = TicketCategory.Adult, CardHolder? Holder = null);
