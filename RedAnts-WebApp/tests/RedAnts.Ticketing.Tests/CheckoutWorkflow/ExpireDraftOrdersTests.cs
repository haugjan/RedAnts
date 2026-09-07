using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.CheckoutWorkflow;
using RedAnts.Features.Ticketing.Ports;
using Xunit;

namespace RedAnts.Ticketing.Tests.CheckoutWorkflow;

public class ExpireDraftOrdersTests
{
    [Fact]
    public async Task Old_drafts_are_cancelled_and_released_while_fresh_and_paid_orders_stay()
    {
        var fixture = new CheckoutFixture();
        var stale = await fixture.PlacedDraftAsync(CheckoutFixture.CartWithTickets(2));
        var paid = await fixture.PlacedDraftAsync(CheckoutFixture.CartWithTickets(1));
        paid.MarkPaid();
        var fresh = await fixture.PlacedDraftAsync(CheckoutFixture.CartWithTickets(3));
        Assert.Equal(6, fixture.EventReserved);

        var expired = await fixture.ExpireDraftOrders.HandleAsync(new ExpireDraftOrders.Command(DateTime.UtcNow.AddDays(-1), CreatedBefore(fresh, stale)));

        Assert.Equal(1, expired);
        Assert.Equal(OrderStatus.Cancelled, stale.Status);
        Assert.Equal(OrderStatus.Paid, paid.Status);
        Assert.Equal(OrderStatus.Draft, fresh.Status);
        Assert.Equal(4, fixture.EventReserved);
        Assert.Contains(fixture.OrderLog.Entries, e => e.OrderId == stale.Id && e.Note == "Reservation abgelaufen");
    }

    [Fact]
    public async Task Nothing_to_expire_returns_zero()
    {
        var fixture = new CheckoutFixture();
        await fixture.PlacedDraftAsync();

        var expired = await fixture.ExpireDraftOrders.HandleAsync(new ExpireDraftOrders.Command(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddHours(-1)));

        Assert.Equal(0, expired);
    }

    [Fact]
    public async Task A_draft_that_Payrexx_confirmed_meanwhile_is_fulfilled_instead_of_expired()
    {
        var fixture = new CheckoutFixture();
        var paidLate = await fixture.PlacedDraftAsync(CheckoutFixture.CartWithTickets(2));
        fixture.Payrexx.Status = PayrexxStatus.Confirmed;

        var expired = await fixture.ExpireDraftOrders.HandleAsync(new ExpireDraftOrders.Command(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddHours(1)));

        Assert.Equal(0, expired);
        Assert.Equal(OrderStatus.Paid, paidLate.Status);
        Assert.Equal(2, fixture.Tickets.Stored.Count);
        Assert.Equal(0, fixture.EventReserved);
    }

    [Fact]
    public async Task A_draft_whose_Payrexx_status_cannot_be_read_is_left_alone()
    {
        var fixture = new CheckoutFixture();
        var unknown = await fixture.PlacedDraftAsync(CheckoutFixture.CartWithTickets(1));
        fixture.Payrexx.ThrowOnStatus = true;

        var expired = await fixture.ExpireDraftOrders.HandleAsync(new ExpireDraftOrders.Command(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddHours(1)));

        Assert.Equal(0, expired);
        Assert.Equal(OrderStatus.Draft, unknown.Status);
        Assert.Equal(1, fixture.EventReserved);
    }

    private static DateTime CreatedBefore(Order fresh, Order stale)
    {
        Assert.True(stale.CreatedAt <= fresh.CreatedAt);
        return stale.CreatedAt.AddTicks(1) < fresh.CreatedAt ? stale.CreatedAt.AddTicks(1) : fresh.CreatedAt;
    }
}
