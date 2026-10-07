using Xunit;

namespace RedAnts.Ticketing.Tests.Checkout;

public class OrderFulfillmentTests
{
    [Fact]
    public async Task Tickets_and_journal_are_written_in_one_unit_of_work()
    {
        var fixture = new CheckoutFixture();
        var order = await fixture.PlacedDraftAsync();

        var fulfilled = await fixture.Fulfillment.FulfillAsync(order.Id);

        Assert.True(fulfilled);
        Assert.Equal(2, fixture.UnitOfWork.Committed);
        Assert.Equal(0, fixture.UnitOfWork.RolledBack);
        Assert.Equal(2, fixture.Tickets.Stored.Count);
        Assert.Single(fixture.Mailer.Sent);
        Assert.Equal(0, fixture.EventReserved);
    }

    [Fact]
    public async Task A_guest_without_a_name_still_gets_the_tickets()
    {
        var fixture = new CheckoutFixture();
        var order = await fixture.PlacedDraftAsync(billing: CheckoutFixture.GuestBilling());

        var fulfilled = await fixture.Fulfillment.FulfillAsync(order.Id);

        Assert.True(fulfilled);
        Assert.Equal(2, fixture.Tickets.Stored.Count);
        Assert.All(fixture.Tickets.Stored, ticket => Assert.Null(ticket.Buyer));
        Assert.Single(fixture.Mailer.Sent);
    }

    [Fact]
    public async Task A_failing_ticket_write_stops_before_the_mail_and_keeps_the_reservation()
    {
        var fixture = new CheckoutFixture();
        var order = await fixture.PlacedDraftAsync();
        fixture.Tickets.FailingSaveNumber = 2;

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Fulfillment.FulfillAsync(order.Id));

        Assert.Equal(1, fixture.UnitOfWork.RolledBack);
        Assert.Empty(fixture.Mailer.Sent);
        Assert.Empty(fixture.OrderItems.Saved);
        Assert.Empty(fixture.Newsletter.Subscriptions);
        Assert.Equal(2, fixture.EventReserved);
    }

    [Fact]
    public async Task A_newsletter_signup_that_is_new_notifies_the_office()
    {
        var fixture = new CheckoutFixture();
        var order = await fixture.PlacedDraftAsync(subscribeNewsletter: true);

        await fixture.Fulfillment.FulfillAsync(order.Id);

        var subscription = Assert.Single(fixture.Newsletter.Subscriptions);
        var notification = Assert.Single(fixture.NewsletterNotifier.Notifications);
        Assert.Equal(subscription.Email, notification.Email);
        Assert.Equal(subscription.Name, notification.Name);
        Assert.Equal(subscription.Source, notification.Source);
    }

    [Fact]
    public async Task An_address_already_on_the_list_is_not_notified_again()
    {
        var fixture = new CheckoutFixture();
        var first = await fixture.PlacedDraftAsync(subscribeNewsletter: true);
        await fixture.Fulfillment.FulfillAsync(first.Id);
        var second = await fixture.PlacedDraftAsync(subscribeNewsletter: true);

        await fixture.Fulfillment.FulfillAsync(second.Id);

        Assert.Single(fixture.Newsletter.Subscriptions);
        Assert.Single(fixture.NewsletterNotifier.Notifications);
    }

    [Fact]
    public async Task Without_the_newsletter_box_nothing_is_subscribed_or_notified()
    {
        var fixture = new CheckoutFixture();
        var order = await fixture.PlacedDraftAsync();

        await fixture.Fulfillment.FulfillAsync(order.Id);

        Assert.Empty(fixture.Newsletter.Subscriptions);
        Assert.Empty(fixture.NewsletterNotifier.Notifications);
    }
}
