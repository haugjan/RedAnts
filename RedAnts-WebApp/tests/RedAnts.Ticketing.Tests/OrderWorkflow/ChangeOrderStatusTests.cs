using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.OrderWorkflow;
using RedAnts.Ticketing.Tests.CheckoutWorkflow;
using Xunit;

namespace RedAnts.Ticketing.Tests.OrderWorkflow;

public class ChangeOrderStatusTests
{
    private readonly InMemoryOrders _orders = new();
    private readonly RecordingOrderLog _log = new();
    private readonly RecordingOrderTickets _tickets = new();

    private ChangeOrderStatus.Handler Handler => new(_orders, _log, _tickets);

    [Fact]
    public async Task Cancelling_a_paid_order_deactivates_its_tickets_and_logs_the_count()
    {
        var order = await OrderFixtures.PaidOrderAsync(_orders);

        var result = await Handler.HandleAsync(new ChangeOrderStatus.Command(order.Id, OrderStatus.Cancelled, "admin"));

        Assert.True(result.Changed);
        Assert.Equal(2, result.DeactivatedTickets);
        Assert.Equal(OrderStatus.Cancelled, (await _orders.GetByIdAsync(order.Id))!.Status);
        Assert.Equal([order.Id], _tickets.DeactivatedOrders);
        var entry = Assert.Single(_log.Entries);
        Assert.Equal((order.Id, OrderStatus.Cancelled, "admin", "Admin-Änderung · 2 Ticket(s) deaktiviert"), entry);
    }

    [Fact]
    public async Task Marking_a_draft_paid_logs_without_deactivating()
    {
        var order = await OrderFixtures.DraftOrderAsync(_orders);

        var result = await Handler.HandleAsync(new ChangeOrderStatus.Command(order.Id, OrderStatus.Paid, "admin"));

        Assert.True(result.Changed);
        Assert.Equal(0, result.DeactivatedTickets);
        Assert.Empty(_tickets.DeactivatedOrders);
        Assert.Equal("Admin-Änderung", Assert.Single(_log.Entries).Note);
        Assert.Equal(OrderStatus.Paid, (await _orders.GetByIdAsync(order.Id))!.Status);
    }

    [Fact]
    public async Task The_same_status_is_a_no_op()
    {
        var order = await OrderFixtures.PaidOrderAsync(_orders);

        var result = await Handler.HandleAsync(new ChangeOrderStatus.Command(order.Id, OrderStatus.Paid, "admin"));

        Assert.False(result.Changed);
        Assert.Empty(_log.Entries);
        Assert.Empty(_tickets.DeactivatedOrders);
    }

    [Fact]
    public async Task An_unsupported_target_is_rejected()
    {
        var order = await OrderFixtures.PaidOrderAsync(_orders);

        await Assert.ThrowsAsync<DomainException>(() =>
            Handler.HandleAsync(new ChangeOrderStatus.Command(order.Id, OrderStatus.PartiallyRefunded, "admin")));
        Assert.Empty(_log.Entries);
    }

    [Fact]
    public async Task An_unknown_order_is_rejected() =>
        await Assert.ThrowsAsync<DomainException>(() =>
            Handler.HandleAsync(new ChangeOrderStatus.Command(999, OrderStatus.Paid, "admin")));
}
