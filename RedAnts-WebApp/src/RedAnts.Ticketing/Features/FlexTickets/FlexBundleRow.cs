using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.FlexTickets;

public sealed record FlexBundleRow(
    int Id,
    int SeasonId,
    TicketCategory Category,
    string Reference,
    DateTimeOffset CreatedAt,
    int TicketCount,
    int RedeemedCount,
    string? CreatedByName = null,
    string? CreatedByEmail = null);
