using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.EventBundles;

public sealed record EventBundleRow(
    int Id,
    int EventId,
    TicketCategory Category,
    string Reference,
    DateTimeOffset CreatedAt,
    int TicketCount,
    int RedeemedCount,
    string? CreatedByName = null,
    string? CreatedByEmail = null);
