using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Orders;

public sealed record OrderAddOnRow(
    int Id,
    int OrderId,
    string OrderNumber,
    DateTimeOffset CreatedAt,
    OrderStatus OrderStatus,
    string BuyerName,
    string Email,
    string Label,
    string CategoryName,
    int Quantity,
    decimal Price,
    bool Delivered);

public sealed record OrderAddOnsForAdmin(int Total, int DeliveredCount, int TotalQuantity, IReadOnlyList<OrderAddOnRow> AddOns)
{
    public static readonly OrderAddOnsForAdmin Empty = new(0, 0, 0, []);
}
