using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Orders;

public sealed record OrderListRow(
    int OrderId,
    string OrderNumber,
    DateTimeOffset CreatedAt,
    OrderStatus Status,
    decimal TotalGross,
    BuyerType BuyerType,
    string BuyerName,
    string Street,
    string? AddressLine2,
    string PostalCode,
    string City,
    string Country,
    string Email,
    int EventTicketCount,
    string EventTicketSummary,
    int SeasonPassCount,
    string SeasonPassSummary,
    int FlexTicketCount,
    string FlexTicketSummary,
    PaymentSource? PaymentSource,
    decimal RefundedAmount);

public sealed record OrdersForAdmin(int Total, IReadOnlyList<OrderListRow> Orders)
{
    public static readonly OrdersForAdmin Empty = new(0, []);
}
