using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Catalog;
using RedAnts.Ticketing.Features.Checkout;
using RedAnts.Ticketing.Features.Orders;
using RedAnts.Ticketing.Tests.Checkout;
using PaymentMethod = RedAnts.Ticketing.Domain.Sales.PaymentMethod;

namespace RedAnts.Ticketing.Tests.Orders;

internal sealed class RecordingOrderRefunds : IOrderRefunds
{
    private int _nextId = 1;

    public List<OrderRefund> Stored { get; } = [];
    public List<(int RefundId, string? PayrexxRefundId, string? By)> Confirmations { get; } = [];
    public List<(int RefundId, string? Error)> Failures { get; } = [];
    public decimal TotalGross { get; set; } = 100m;

    public Task<RefundSummary> GetSummaryAsync(int orderId)
    {
        var confirmed = Stored.Where(r => r.OrderId == orderId && r.Status == RefundStatus.Confirmed).Sum(r => r.Amount.Amount);
        var reserved = Stored.Where(r => r.OrderId == orderId && r.Status == RefundStatus.Pending).Sum(r => r.Amount.Amount);
        return Task.FromResult(new RefundSummary(orderId, Money.Of(TotalGross), Money.Of(confirmed), Money.Of(reserved),
            Money.Stored(TotalGross - confirmed - reserved)));
    }

    public Task<OrderRefund> CreateAsync(int orderId, Money amount, RefundMethod method, RefundStatus initialStatus,
        string? reference, string? reason, string? createdBy)
    {
        var id = _nextId++;
        var refund = OrderRefund.FromPersistence(id, $"R-{id:000}", orderId, amount.Amount, 0m, 0m, "CHF", method, initialStatus,
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
        Stored.Add(OrderRefund.FromPersistence(current.Id, current.RefundNumber, current.OrderId, current.Amount.Amount, current.VatRate,
            current.VatAmount.Amount, current.Currency, current.Method, status, payrexxRefundId, current.Reference, current.Reason,
            current.CreatedBy, current.CreatedAt));
    }
}

internal sealed class RecordingOrderTickets : IOrderTickets
{
    public List<int> DeactivatedOrders { get; } = [];
    public int TicketsPerOrder { get; set; } = 2;
    public bool Throws { get; set; }

    public Task<int> DeactivateByOrderAsync(int orderId)
    {
        if (Throws) throw new InvalidOperationException("ticket table unavailable");
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

    public static async Task<Order> PaidOrderAsync(InMemoryOrderRepository orders, decimal total = 100m, string? gatewayId = null)
    {
        var order = Order.Create(await orders.NextOrderNumberAsync(), Billing(), Money.Of(total), 0m, PaymentMethod.Payrexx, null,
            paymentSource: PaymentSource.Online);
        order.MarkPaid();
        order.SetPayrexxGatewayId(gatewayId);
        return await orders.SaveAsync(order);
    }

    public static async Task<Order> DraftOrderAsync(InMemoryOrderRepository orders, decimal total = 100m)
    {
        var order = Order.Create(await orders.NextOrderNumberAsync(), Billing(), Money.Of(total), 0m, PaymentMethod.Payrexx, null,
            paymentSource: PaymentSource.Online);
        return await orders.SaveAsync(order);
    }
}

internal sealed class StubOrderListReader : IOrderListReader
{
    public List<OrderListRow> Rows { get; } = [];
    public (int SeasonId, IReadOnlyCollection<int> EventIds)? LastCall { get; private set; }

    public Task<IReadOnlyList<OrderListRow>> GetBySeasonAsync(int seasonId, IReadOnlyCollection<int> eventIds)
    {
        LastCall = (seasonId, eventIds);
        return Task.FromResult<IReadOnlyList<OrderListRow>>(Rows);
    }

    public static OrderListRow Row(int id, string number, string buyer, string email, OrderStatus status = OrderStatus.Paid, string city = "Winterthur") =>
        new(id, number, SwissTime.Timestamp, status, 50m, BuyerType.Private, buyer, "Bahnhofstrasse 1", null, "8400", city, "Schweiz",
            email, 2, "2× Erwachsen", 0, "—", 0, "—", PaymentSource.Online, 0m);
}

internal sealed class StubOrderDetailReader : IOrderDetailReader
{
    public Dictionary<int, OrderDetail> Details { get; } = new();

    public Task<OrderDetail?> GetAsync(int orderId) =>
        Task.FromResult(Details.TryGetValue(orderId, out var detail) ? detail : null);
}

internal sealed class StubOrderAddOnListReader : IOrderAddOnListReader
{
    public List<OrderAddOnRow> Rows { get; } = [];
    public int? LastSeasonId { get; private set; }

    public Task<IReadOnlyList<OrderAddOnRow>> GetBySeasonAsync(int seasonId)
    {
        LastSeasonId = seasonId;
        return Task.FromResult<IReadOnlyList<OrderAddOnRow>>(Rows);
    }

    public static OrderAddOnRow Row(int id, string number, string buyer, string label, int quantity, bool delivered) =>
        new(id, id * 10, number, SwissTime.Timestamp, OrderStatus.Paid, buyer, $"{buyer.ToLowerInvariant()}@example.ch", label, "Erwachsen", quantity, 5m, delivered);
}

internal sealed class StubEvents : IEventReader
{
    public List<Event> Events { get; } = [];

    public static Event InSeason(int id, int seasonId) =>
        Event.FromPersistence(id, $"Spiel {id}", null, seasonId, new DateOnly(2026, 10, 3), new TimeOnly(18, 0), false, 1, EventStatus.Open,
            null, null, null, null);

    public Task<IReadOnlyList<Event>> GetAllAsync() => Task.FromResult<IReadOnlyList<Event>>(Events);

    public Task<IReadOnlyList<Event>> GetPublicOpenAsync() => Task.FromResult<IReadOnlyList<Event>>(Events);

    public Task<IReadOnlyList<Event>> GetUpcomingForScanningAsync() => Task.FromResult<IReadOnlyList<Event>>(Events);

    public Task<IReadOnlyList<Event>> GetBySeasonAsync(int seasonId) =>
        Task.FromResult<IReadOnlyList<Event>>(Events.Where(e => e.SeasonId == seasonId).ToList());

    public Task<Event?> FindByIdAsync(int id) => Task.FromResult(Events.FirstOrDefault(e => e.Id == id));
}
