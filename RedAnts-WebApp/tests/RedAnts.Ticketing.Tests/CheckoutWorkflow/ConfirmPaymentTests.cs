using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.CheckoutWorkflow;
using RedAnts.Features.Ticketing.Ports;
using Xunit;

namespace RedAnts.Ticketing.Tests.CheckoutWorkflow;

public class ConfirmPaymentTests
{
    [Fact]
    public async Task Unknown_order_is_not_found()
    {
        var fixture = new CheckoutFixture();

        var result = await fixture.ConfirmPayment.HandleAsync(new ConfirmPayment.Command(999));

        Assert.Equal(new ConfirmPayment.Result(false, false, false), result);
    }

    [Fact]
    public async Task Confirmed_payment_fulfils_the_order_exactly_once()
    {
        var fixture = new CheckoutFixture();
        var order = await fixture.PlacedDraftAsync(CheckoutFixture.CartWithTickets(2));
        fixture.Payrexx.Status = PayrexxStatus.Confirmed;

        var first = await fixture.ConfirmPayment.HandleAsync(new ConfirmPayment.Command(order.Id));
        var second = await fixture.ConfirmPayment.HandleAsync(new ConfirmPayment.Command(order.Id));

        Assert.Equal(new ConfirmPayment.Result(true, true, false), first);
        Assert.Equal(new ConfirmPayment.Result(true, true, false), second);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(2, fixture.Tickets.Stored.Count);
        Assert.Single(fixture.Mailer.Sent);
        Assert.Equal(0, fixture.EventReserved);
        Assert.Equal(1, fixture.Payrexx.StatusCalls);
    }

    [Fact]
    public async Task Cancelled_payment_cancels_the_draft_and_frees_the_seats()
    {
        var fixture = new CheckoutFixture();
        var order = await fixture.PlacedDraftAsync(CheckoutFixture.CartWithTickets(2));
        fixture.Payrexx.Status = PayrexxStatus.Declined;

        var result = await fixture.ConfirmPayment.HandleAsync(new ConfirmPayment.Command(order.Id));

        Assert.Equal(new ConfirmPayment.Result(true, false, true), result);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(0, fixture.EventReserved);
        Assert.Equal(0, fixture.TierReserved);
        Assert.Contains(fixture.OrderLog.Entries, e => e.OrderId == order.Id && e.Status == OrderStatus.Cancelled && e.By == "Payrexx");
        Assert.Empty(fixture.Tickets.Stored);
    }

    [Fact]
    public async Task Pending_payment_changes_nothing()
    {
        var fixture = new CheckoutFixture();
        var order = await fixture.PlacedDraftAsync();

        var result = await fixture.ConfirmPayment.HandleAsync(new ConfirmPayment.Command(order.Id));

        Assert.Equal(new ConfirmPayment.Result(true, false, false), result);
        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Equal(2, fixture.EventReserved);
    }

    [Fact]
    public async Task Provider_outage_is_reported_as_pending()
    {
        var fixture = new CheckoutFixture();
        var order = await fixture.PlacedDraftAsync();
        fixture.Payrexx.ThrowOnStatus = true;

        var result = await fixture.ConfirmPayment.HandleAsync(new ConfirmPayment.Command(order.Id));

        Assert.Equal(new ConfirmPayment.Result(true, false, false), result);
        Assert.Equal(OrderStatus.Draft, order.Status);
    }

    [Fact]
    public async Task Paid_order_answers_without_asking_the_provider()
    {
        var fixture = new CheckoutFixture();
        var order = await fixture.PlacedDraftAsync();
        order.MarkPaid();

        var result = await fixture.ConfirmPayment.HandleAsync(new ConfirmPayment.Command(order.Id));

        Assert.Equal(new ConfirmPayment.Result(true, true, false), result);
        Assert.Equal(0, fixture.Payrexx.StatusCalls);
    }
}
