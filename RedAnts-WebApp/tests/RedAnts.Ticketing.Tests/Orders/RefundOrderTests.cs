using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Checkout;
using RedAnts.Ticketing.Features.Orders;
using RedAnts.Ticketing.Tests.Checkout;
using Xunit;

namespace RedAnts.Ticketing.Tests.Orders;

public class RefundOrderTests
{
    private readonly InMemoryOrderRepository _orders = new();
    private readonly RecordingOrderRefunds _refunds = new();
    private readonly RefundingPayrexx _payrexx = new();
    private readonly RecordingOrderTickets _tickets = new();
    private readonly RecordingUnitOfWork _unitOfWork = new();

    private RefundOrder.Handler Handler => new(_orders, _refunds, _payrexx, _tickets, _unitOfWork);

    private static RefundRequest Manual(int orderId, decimal amount, bool deactivate = false) =>
        new(orderId, amount, RefundMethod.Cash, false, "Beleg 1", "Kulanz", deactivate, "admin");

    private static RefundRequest ViaPayrexx(int orderId, decimal amount) =>
        new(orderId, amount, RefundMethod.Payrexx, true, null, "Absage", false, "admin");

    [Fact]
    public async Task A_manual_refund_is_recorded_as_confirmed()
    {
        var order = await OrderFixtures.PaidOrderAsync(_orders);

        var result = await Handler.HandleAsync(new RefundOrder.Command(Manual(order.Id, 40m)));

        var refund = Assert.Single(_refunds.Stored);
        Assert.Equal(RefundStatus.Confirmed, refund.Status);
        Assert.Equal(RefundMethod.Cash, refund.Method);
        Assert.Equal(40m, refund.Amount);
        Assert.Equal("Beleg 1", refund.Reference);
        Assert.Equal(refund.RefundNumber, result.RefundNumber);
        Assert.Equal(40m, result.RefundedTotal);
        Assert.Equal(60m, result.Remaining);
        Assert.Equal(0, result.DeactivatedTickets);
        Assert.Empty(_payrexx.Refunds);
    }

    [Fact]
    public async Task Deactivating_tickets_is_reported()
    {
        var order = await OrderFixtures.PaidOrderAsync(_orders);

        var result = await Handler.HandleAsync(new RefundOrder.Command(Manual(order.Id, 100m, deactivate: true)));

        Assert.Equal(2, result.DeactivatedTickets);
        Assert.Equal([order.Id], _tickets.DeactivatedOrders);
        Assert.Equal(0m, result.Remaining);
    }

    [Fact]
    public async Task A_Payrexx_refund_reserves_calls_the_gateway_and_confirms()
    {
        var order = await OrderFixtures.PaidOrderAsync(_orders, gatewayId: "gw-7");

        var result = await Handler.HandleAsync(new RefundOrder.Command(ViaPayrexx(order.Id, 12.5m)));

        Assert.Equal([("gw-7", 1250)], _payrexx.Refunds);
        var refund = Assert.Single(_refunds.Stored);
        Assert.Equal(RefundStatus.Confirmed, refund.Status);
        Assert.Equal("pr-1", refund.PayrexxRefundId);
        Assert.Equal((refund.Id, "pr-1", "admin"), Assert.Single(_refunds.Confirmations));
        Assert.Equal(12.5m, result.RefundedTotal);
    }

    [Fact]
    public async Task A_Payrexx_refund_of_an_order_without_gateway_is_rejected()
    {
        var order = await OrderFixtures.PaidOrderAsync(_orders);

        await Assert.ThrowsAsync<DomainException>(() => Handler.HandleAsync(new RefundOrder.Command(ViaPayrexx(order.Id, 10m))));
        Assert.Empty(_refunds.Stored);
    }

    [Fact]
    public async Task A_Payrexx_refund_is_marked_failed_when_the_gateway_throws()
    {
        var order = await OrderFixtures.PaidOrderAsync(_orders, gatewayId: "gw-7");
        _payrexx.ThrowOnRefund = true;

        var ex = await Assert.ThrowsAsync<DomainException>(() => Handler.HandleAsync(new RefundOrder.Command(ViaPayrexx(order.Id, 10m))));

        Assert.Contains("payrexx down", ex.Message);
        Assert.Equal(RefundStatus.Failed, Assert.Single(_refunds.Stored).Status);
        Assert.Equal("payrexx down", Assert.Single(_refunds.Failures).Error);
    }

    [Fact]
    public async Task A_Payrexx_refund_is_marked_failed_when_the_gateway_reports_failure()
    {
        var order = await OrderFixtures.PaidOrderAsync(_orders, gatewayId: "gw-7");
        _payrexx.RefundResult = new PayrexxRefundResult(false, "insufficient balance");

        var ex = await Assert.ThrowsAsync<DomainException>(() => Handler.HandleAsync(new RefundOrder.Command(ViaPayrexx(order.Id, 10m))));

        Assert.Contains("insufficient balance", ex.Message);
        Assert.Equal(RefundStatus.Failed, Assert.Single(_refunds.Stored).Status);
        Assert.Empty(_refunds.Confirmations);
    }

    [Fact]
    public async Task An_unpaid_order_cannot_be_refunded()
    {
        var order = await OrderFixtures.DraftOrderAsync(_orders);

        await Assert.ThrowsAsync<DomainException>(() => Handler.HandleAsync(new RefundOrder.Command(Manual(order.Id, 10m))));
        Assert.Empty(_refunds.Stored);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task A_non_positive_amount_is_rejected(decimal amount)
    {
        var order = await OrderFixtures.PaidOrderAsync(_orders);

        await Assert.ThrowsAsync<DomainException>(() => Handler.HandleAsync(new RefundOrder.Command(Manual(order.Id, amount))));
        Assert.Empty(_refunds.Stored);
    }

    [Fact]
    public async Task An_amount_above_the_remaining_rest_is_rejected()
    {
        var order = await OrderFixtures.PaidOrderAsync(_orders);
        await Handler.HandleAsync(new RefundOrder.Command(Manual(order.Id, 70m)));

        var ex = await Assert.ThrowsAsync<DomainException>(() => Handler.HandleAsync(new RefundOrder.Command(Manual(order.Id, 40m))));

        Assert.Contains("30.00", ex.Message);
        Assert.Single(_refunds.Stored);
    }

    [Fact]
    public async Task A_partially_refunded_order_can_be_refunded_again()
    {
        var order = await OrderFixtures.PaidOrderAsync(_orders);
        order.ApplyRefundTotal(30m);
        await _orders.SaveAsync(order);

        var result = await Handler.HandleAsync(new RefundOrder.Command(Manual(order.Id, 20m)));

        Assert.Equal(20m, result.RefundedTotal);
        Assert.Equal(OrderStatus.PartiallyRefunded, result.Status);
    }

    [Fact]
    public async Task The_refund_row_and_the_ticket_deactivation_share_one_unit_of_work()
    {
        var order = await OrderFixtures.PaidOrderAsync(_orders);

        await Handler.HandleAsync(new RefundOrder.Command(Manual(order.Id, 40m, deactivate: true)));

        Assert.Equal(1, _unitOfWork.Committed);
        Assert.Equal(0, _unitOfWork.RolledBack);
        Assert.Single(_refunds.Stored);
        Assert.Single(_tickets.DeactivatedOrders);
    }

    [Fact]
    public async Task A_failing_ticket_deactivation_rolls_the_refund_back()
    {
        var order = await OrderFixtures.PaidOrderAsync(_orders);
        _tickets.Throws = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Handler.HandleAsync(new RefundOrder.Command(Manual(order.Id, 40m, deactivate: true))));

        Assert.Equal(1, _unitOfWork.RolledBack);
        Assert.Equal(0, _unitOfWork.Committed);
        Assert.Empty(_tickets.DeactivatedOrders);
    }

    [Fact]
    public async Task The_payrexx_call_runs_outside_the_unit_of_work()
    {
        var order = await OrderFixtures.PaidOrderAsync(_orders, gatewayId: "gw-1");

        await Handler.HandleAsync(new RefundOrder.Command(ViaPayrexx(order.Id, 40m)));

        Assert.Single(_payrexx.Refunds);
        Assert.Equal(1, _unitOfWork.Committed);
        Assert.Single(_refunds.Confirmations);
    }

    [Fact]
    public async Task A_failing_payrexx_refund_opens_no_unit_of_work()
    {
        var order = await OrderFixtures.PaidOrderAsync(_orders, gatewayId: "gw-1");
        _payrexx.ThrowOnRefund = true;

        await Assert.ThrowsAsync<DomainException>(() => Handler.HandleAsync(new RefundOrder.Command(ViaPayrexx(order.Id, 40m))));

        Assert.Equal(0, _unitOfWork.Started);
        Assert.Single(_refunds.Failures);
    }
}
