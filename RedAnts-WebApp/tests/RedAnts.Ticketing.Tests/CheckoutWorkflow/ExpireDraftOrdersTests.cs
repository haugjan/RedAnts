using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.CheckoutWorkflow;
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

        var expired = await fixture.ExpireDraftOrders.HandleAsync(new ExpireDraftOrders.Command(CreatedBefore(fresh, stale)));

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

        var expired = await fixture.ExpireDraftOrders.HandleAsync(new ExpireDraftOrders.Command(DateTime.UtcNow.AddHours(-1)));

        Assert.Equal(0, expired);
    }

    private static DateTime CreatedBefore(Order fresh, Order stale)
    {
        Assert.True(stale.CreatedAt <= fresh.CreatedAt);
        return stale.CreatedAt.AddTicks(1) < fresh.CreatedAt ? stale.CreatedAt.AddTicks(1) : fresh.CreatedAt;
    }
}
