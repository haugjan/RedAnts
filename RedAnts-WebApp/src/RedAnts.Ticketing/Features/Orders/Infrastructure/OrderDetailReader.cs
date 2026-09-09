using NPoco;
using RedAnts.Ticketing.Domain.Sales;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Features.Orders.Infrastructure;

public sealed class OrderDetailReader(IScopeProvider scopeProvider) : IOrderDetailReader
{
    public async Task<OrderDetail?> GetAsync(int orderId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        var total = await db.ExecuteScalarAsync<decimal?>("SELECT TotalGross FROM Orders WHERE Id = @0", orderId);
        if (total is null) return null;

        var confirmed = await SumAsync(db, orderId, RefundStatus.Confirmed);
        var reserved = await SumAsync(db, orderId, RefundStatus.Pending, RefundStatus.Confirmed);
        var items = await db.FetchAsync<OrderItemRecord>("WHERE OrderId = @0 ORDER BY Id", orderId);
        var refunds = await db.FetchAsync<OrderRefundRecord>("WHERE OrderId = @0 ORDER BY Id", orderId);
        var log = await db.FetchAsync<OrderStatusLogRecord>("WHERE OrderId = @0 ORDER BY Id", orderId);

        return new OrderDetail(
            orderId,
            total.Value,
            confirmed,
            total.Value - reserved,
            items.Select(i => new OrderItemRow((OrderItemKind)i.Kind, i.Label, i.Quantity, i.UnitPrice,
                decimal.Round(i.UnitPrice * i.Quantity, 2))).ToList(),
            refunds.Select(r => new OrderRefundRow(r.RefundNumber, r.CreatedAt, r.Amount, (RefundMethod)r.Method,
                (RefundStatus)r.Status, r.CreatedBy, r.Reference)).ToList(),
            log.Select(l => new OrderLogRow((OrderStatus)l.ToStatus, l.ChangedBy, l.OccurredAt, l.Note)).ToList());
    }

    private static Task<decimal> SumAsync(IDatabase db, int orderId, params RefundStatus[] statuses) =>
        db.ExecuteScalarAsync<decimal>(
            "SELECT ISNULL(SUM(Amount), 0) FROM OrderRefunds WHERE OrderId = @0 AND Status IN (@1)",
            orderId, statuses.Select(s => (int)s).ToArray());
}
