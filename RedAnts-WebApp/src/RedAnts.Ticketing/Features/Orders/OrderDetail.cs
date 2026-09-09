using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Orders;

public sealed record OrderItemRow(OrderItemKind Kind, string Label, int Quantity, decimal UnitPrice, decimal LineTotal);

public sealed record OrderRefundRow(
    string RefundNumber, DateTimeOffset CreatedAt, decimal Amount, RefundMethod Method, RefundStatus Status,
    string? CreatedBy, string? Reference);

public sealed record OrderLogRow(OrderStatus ToStatus, string? ChangedBy, DateTimeOffset OccurredAt, string? Note);

public sealed record OrderDetail(
    int OrderId,
    decimal TotalGross,
    decimal RefundedConfirmed,
    decimal Remaining,
    IReadOnlyList<OrderItemRow> Items,
    IReadOnlyList<OrderRefundRow> Refunds,
    IReadOnlyList<OrderLogRow> Log)
{
    public static OrderDetail Empty(int orderId) => new(orderId, 0m, 0m, 0m, [], [], []);
}
