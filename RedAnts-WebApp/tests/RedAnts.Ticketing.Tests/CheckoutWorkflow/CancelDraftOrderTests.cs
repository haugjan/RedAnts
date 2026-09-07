using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.CheckoutWorkflow;
using Xunit;

namespace RedAnts.Ticketing.Tests.CheckoutWorkflow;

public class CancelDraftOrderTests
{
    [Fact]
    public async Task Draft_is_cancelled_and_its_reservation_released()
    {
        var fixture = new CheckoutFixture();
        var order = await fixture.PlacedDraftAsync(CheckoutFixture.CartWithTickets(3));
        Assert.Equal(3, fixture.EventReserved);

        var cancelled = await fixture.CancelDraftOrder.HandleAsync(new CancelDraftOrder.Command(order.Id, "Zahlung abgebrochen"));

        Assert.True(cancelled);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(0, fixture.EventReserved);
        Assert.Equal(0, fixture.TierReserved);
        Assert.Contains(fixture.OrderLog.Entries, e => e.OrderId == order.Id && e.Status == OrderStatus.Cancelled && e.Note == "Zahlung abgebrochen");
    }

    [Fact]
    public async Task Paid_order_is_left_alone()
    {
        var fixture = new CheckoutFixture();
        var order = await fixture.PlacedDraftAsync();
        order.MarkPaid();

        var cancelled = await fixture.CancelDraftOrder.HandleAsync(new CancelDraftOrder.Command(order.Id, "irrelevant"));

        Assert.False(cancelled);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(2, fixture.EventReserved);
    }

    [Fact]
    public async Task Unknown_order_is_not_cancelled()
    {
        var fixture = new CheckoutFixture();

        Assert.False(await fixture.CancelDraftOrder.HandleAsync(new CancelDraftOrder.Command(42, "x")));
    }
}
