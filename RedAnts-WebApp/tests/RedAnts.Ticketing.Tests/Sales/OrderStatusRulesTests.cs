using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Domain.Sales;
using Xunit;
using PaymentMethod = RedAnts.Ticketing.Domain.Sales.PaymentMethod;

namespace RedAnts.Ticketing.Tests.Sales;

public class OrderStatusRulesTests
{
    private static Order Draft() => Order.Create("2026-000001", BillingAddress.Create(
        BuyerType.Private, "Anna", "Muster", null, "Bahnhofstrasse 1", null, "8400", "Winterthur", "Schweiz", "anna@example.ch", null),
        Money.Of(50m), 0m, PaymentMethod.Payrexx, null);

    private static Order Paid()
    {
        var order = Draft();
        order.MarkPaid();
        return order;
    }

    [Fact]
    public void ChangeStatus_to_the_same_status_changes_nothing()
    {
        var order = Paid();
        Assert.False(order.ChangeStatus(OrderStatus.Paid));
        Assert.Equal(OrderStatus.Paid, order.Status);
    }

    [Fact]
    public void ChangeStatus_to_cancelled_deactivates_tickets()
    {
        var order = Paid();
        Assert.True(order.ChangeStatus(OrderStatus.Cancelled));
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.True(order.DeactivatesTickets);
    }

    [Fact]
    public void ChangeStatus_back_to_draft_clears_the_payment()
    {
        var order = Paid();
        Assert.True(order.ChangeStatus(OrderStatus.Draft));
        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Null(order.PaidAt);
        Assert.False(order.DeactivatesTickets);
    }

    [Fact]
    public void ChangeStatus_rejects_partially_refunded_as_a_target() =>
        Assert.Throws<DomainException>(() => Paid().ChangeStatus(OrderStatus.PartiallyRefunded));

    [Fact]
    public void Cancelled_orders_cannot_be_marked_paid()
    {
        var order = Draft();
        order.Cancel();
        Assert.Throws<DomainException>(() => order.ChangeStatus(OrderStatus.Paid));
    }

    [Fact]
    public void Only_paid_or_partially_refunded_orders_are_refundable()
    {
        Assert.True(Paid().IsRefundable);
        var partial = Paid();
        partial.ApplyRefundTotal(Money.Of(10m));
        Assert.True(partial.IsRefundable);
        Assert.False(Draft().IsRefundable);
        Assert.IsType<RefundDenied.NotPaid>(Assert.IsType<CheckResult.Denied>(Draft().RefundBlocker(Money.Of(100m))).Cause);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void A_refund_needs_a_positive_amount(decimal amount) =>
        Assert.IsType<RefundDenied.AmountNotPositive>(Assert.IsType<CheckResult.Denied>(Paid().RefundBlocker(Money.Of(100m), Money.Stored(amount))).Cause);

    [Fact]
    public void A_refund_stays_below_the_remaining_amount()
    {
        var denied = Assert.IsType<CheckResult.Denied>(Paid().RefundBlocker(Money.Of(30m), Money.Of(40m)));

        Assert.IsType<RefundDenied.AmountAboveRemaining>(denied.Cause);
        Assert.Contains("30.00", denied.Cause.Message);
    }

    [Fact]
    public void A_fully_refunded_order_has_nothing_left()
    {
        var order = Paid();
        order.ApplyRefundTotal(Money.Of(10m));

        Assert.IsType<RefundDenied.NothingLeft>(Assert.IsType<CheckResult.Denied>(order.RefundBlocker(Money.Zero)).Cause);
    }

    [Fact]
    public void A_payrexx_refund_needs_an_online_payment()
    {
        Assert.IsType<RefundDenied.NotPaidOnline>(
            Assert.IsType<CheckResult.Denied>(Paid().RefundBlocker(Money.Of(100m), Money.Of(10m), viaPayrexx: true)).Cause);
        Assert.True(Paid().RefundBlocker(Money.Of(100m), Money.Of(10m)).IsAllowed);
    }

    [Fact]
    public void PaidThroughPayrexx_follows_the_gateway_id()
    {
        var order = Paid();
        Assert.False(order.PaidThroughPayrexx);
        order.SetPayrexxGatewayId("gw-1");
        Assert.True(order.PaidThroughPayrexx);
    }
}
