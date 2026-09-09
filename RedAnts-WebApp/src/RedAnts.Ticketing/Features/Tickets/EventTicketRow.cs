using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Admission.Admin;

namespace RedAnts.Ticketing.Features.Tickets;

public sealed record EventTicketRow(
    Guid Uuid,
    string Link,
    int EventId,
    TicketCategory Category,
    decimal Price,
    TicketStatus Status,
    bool Redeemed,
    bool? Inside,
    RedemptionState Redemption,
    DateTimeOffset CreatedAt,
    string? CreatedByName,
    string? CreatedByEmail,
    int? OrderId,
    int? BundleId,
    string? BundleReference,
    TicketType? OriginType,
    Guid? OriginCardUuid,
    string? BuyerName,
    CardHolder? Holder);

public sealed record EventTicketBundleRow(int Id, string Reference, TicketCategory Category, int TicketCount);

public sealed record EventTicketsForAdmin(
    int Total,
    int RedeemedCount,
    int OutsideCount,
    int OpenCount,
    IReadOnlyList<EventTicketRow> Tickets,
    IReadOnlyList<EventTicketBundleRow> Bundles)
{
    public static readonly EventTicketsForAdmin Empty = new(0, 0, 0, 0, [], []);
}
