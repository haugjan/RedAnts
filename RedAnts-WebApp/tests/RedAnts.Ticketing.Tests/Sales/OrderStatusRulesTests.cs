using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using Xunit;
using PaymentMethod = RedAnts.Domain.Ticketing.Sales.PaymentMethod;

namespace RedAnts.Ticketing.Tests.Sales;

public class OrderStatusRulesTests
{
    private static Order Draft() => Order.Create("2026-000001", BillingAddress.Create(
        BuyerType.Private, "Anna", "Muster", null, "Bahnhofstrasse 1", null, "8400", "Winterthur", "Schweiz", "anna@example.ch", null),
        50m, 0m, PaymentMethod.Payrexx, null);

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
        partial.ApplyRefundTotal(10m);
        Assert.True(partial.IsRefundable);
        Assert.False(Draft().IsRefundable);
        Assert.Throws<DomainException>(() => Draft().RequireRefundable());
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
