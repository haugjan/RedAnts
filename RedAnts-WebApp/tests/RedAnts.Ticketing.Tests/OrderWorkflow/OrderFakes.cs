using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Admin;
using RedAnts.Features.Ticketing.Ports;
using RedAnts.Ticketing.Tests.CheckoutWorkflow;
using PaymentMethod = RedAnts.Domain.Ticketing.Sales.PaymentMethod;

namespace RedAnts.Ticketing.Tests.OrderWorkflow;

internal sealed class RecordingOrderRefunds : IOrderRefunds
{
    private int _nextId = 1;

    public List<OrderRefund> Stored { get; } = [];
    public List<(int RefundId, string? PayrexxRefundId, string? By)> Confirmations { get; } = [];
    public List<(int RefundId, string? Error)> Failures { get; } = [];
    public decimal TotalGross { get; set; } = 100m;

    public Task<RefundSummary> GetSummaryAsync(int orderId)
    {
        var confirmed = Stored.Where(r => r.OrderId == orderId && r.Status == RefundStatus.Confirmed).Sum(r => r.Amount);
        var reserved = Stored.Where(r => r.OrderId == orderId && r.Status == RefundStatus.Pending).Sum(r => r.Amount);
        return Task.FromResult(new RefundSummary(orderId, TotalGross, confirmed, reserved, TotalGross - confirmed - reserved));
    }

    public Task<IReadOnlyList<OrderRefund>> GetByOrderAsync(int orderId) =>
        Task.FromResult<IReadOnlyList<OrderRefund>>(Stored.Where(r => r.OrderId == orderId).ToList());

    public Task<OrderRefund> CreateAsync(int orderId, decimal amount, RefundMethod method, RefundStatus initialStatus,
        string? reference, string? reason, string? createdBy)
    {
        var id = _nextId++;
        var refund = OrderRefund.FromPersistence(id, $"R-{id:000}", orderId, amount, 0m, 0m, "CHF", method, initialStatus,
            null, reference, reason, createdBy, SwissTime.Timestamp);
        Stored.Add(refund);
        return Task.FromResult(refund);
    }

    public Task ConfirmAsync(int refundId, string? payrexxRefundId, string? changedBy)
    {
        Confirmations.Add((refundId, payrexxRefundId, changedBy));
        Replace(refundId, RefundStatus.Confirmed, payrexxRefundId);
        return Task.CompletedTask;
    }

    public Task FailAsync(int refundId, string? error)
    {
        Failures.Add((refundId, error));
        Replace(refundId, RefundStatus.Failed, null);
        return Task.CompletedTask;
    }

    private void Replace(int refundId, RefundStatus status, string? payrexxRefundId)
    {
        var current = Stored.Single(r => r.Id == refundId);
        Stored.Remove(current);
        Stored.Add(OrderRefund.FromPersistence(current.Id, current.RefundNumber, current.OrderId, current.Amount, current.VatRate,
            current.VatAmount, current.Currency, current.Method, status, payrexxRefundId, current.Reference, current.Reason,
            current.CreatedBy, current.CreatedAt));
    }
}

internal sealed class RecordingOrderTickets : IOrderTickets
{
    public List<int> DeactivatedOrders { get; } = [];
    public int TicketsPerOrder { get; set; } = 2;

    public Task<int> DeactivateByOrderAsync(int orderId)
    {
        DeactivatedOrders.Add(orderId);
        return Task.FromResult(TicketsPerOrder);
    }
}

internal sealed class RefundingPayrexx : IPayrexxGateway
{
    public bool Enabled { get; set; } = true;
    public bool ThrowOnRefund { get; set; }
    public PayrexxRefundResult RefundResult { get; set; } = new(true, null, "pr-1");
    public List<(string GatewayId, int Cents)> Refunds { get; } = [];

    public Task<PayrexxGatewayResult> CreateGatewayAsync(PayrexxCreateRequest request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<PayrexxStatus> GetGatewayStatusAsync(string gatewayId, CancellationToken cancellationToken = default) =>
        Task.FromResult(PayrexxStatus.Confirmed);

    public Task<PayrexxRefundResult> RefundGatewayAsync(string gatewayId, int amountInCents, CancellationToken cancellationToken = default)
    {
        Refunds.Add((gatewayId, amountInCents));
        if (ThrowOnRefund) throw new InvalidOperationException("payrexx down");
        return Task.FromResult(RefundResult);
    }
}

internal static class OrderFixtures
{
    public static BillingAddress Billing() => BillingAddress.Create(
        BuyerType.Private, "Anna", "Muster", null, "Bahnhofstrasse 1", null, "8400", "Winterthur", "Schweiz", "anna@example.ch", null);

    public static async Task<Order> PaidOrderAsync(InMemoryOrders orders, decimal total = 100m, string? gatewayId = null)
    {
        var order = Order.Create(await orders.NextOrderNumberAsync(), Billing(), total, 0m, PaymentMethod.Payrexx, null,
            paymentSource: PaymentSource.Online);
        order.MarkPaid();
        order.SetPayrexxGatewayId(gatewayId);
        return await orders.SaveAsync(order);
    }

    public static async Task<Order> DraftOrderAsync(InMemoryOrders orders, decimal total = 100m)
    {
        var order = Order.Create(await orders.NextOrderNumberAsync(), Billing(), total, 0m, PaymentMethod.Payrexx, null,
            paymentSource: PaymentSource.Online);
        return await orders.SaveAsync(order);
    }
}
