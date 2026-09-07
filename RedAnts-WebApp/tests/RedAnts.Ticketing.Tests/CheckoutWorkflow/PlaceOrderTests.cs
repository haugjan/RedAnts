using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.CheckoutWorkflow;
using Xunit;

namespace RedAnts.Ticketing.Tests.CheckoutWorkflow;

public class PlaceOrderTests
{
    private static PlaceOrder.Command Command(Cart cart, bool newsletter = false, string? phone = null, CheckoutSource source = CheckoutSource.Checkout) =>
        new(cart, CheckoutFixture.Billing(phone), newsletter, source);

    [Fact]
    public async Task Empty_cart_is_denied_back_to_cart()
    {
        var fixture = new CheckoutFixture();

        var result = await fixture.PlaceOrder.HandleAsync(Command(Cart.Empty()));

        var denied = Assert.IsType<PlaceOrder.Result.Denied>(result);
        Assert.True(denied.BackToCart);
        Assert.Empty(fixture.Orders.Stored);
    }

    [Fact]
    public async Task Full_hall_closes_the_box_office()
    {
        var fixture = new CheckoutFixture();
        fixture.HallFull();

        var result = await fixture.PlaceOrder.HandleAsync(Command(CheckoutFixture.CartWithTickets()));

        var denied = Assert.IsType<PlaceOrder.Result.Denied>(result);
        Assert.StartsWith("Abendkasse geschlossen", denied.Message);
        Assert.True(denied.BackToCart);
        Assert.Equal(0, fixture.EventReserved);
    }

    [Fact]
    public async Task Conversion_only_event_rejects_regular_tickets()
    {
        var fixture = new CheckoutFixture();
        fixture.ConversionRules.ConversionOnlyEvents.Add(CheckoutFixture.EventId);

        var result = await fixture.PlaceOrder.HandleAsync(Command(CheckoutFixture.CartWithTickets()));

        var denied = Assert.IsType<PlaceOrder.Result.Denied>(result);
        Assert.Contains("nur Kartenumwandlung", denied.Message);
        Assert.Empty(fixture.Orders.Stored);
    }

    [Fact]
    public async Task Missing_mobile_number_is_denied_on_the_checkout_form()
    {
        var fixture = new CheckoutFixture();
        fixture.SeasonAddOns.AddOns.Add(SeasonAddOn.FromPersistence(5, CheckoutFixture.SeasonId, "Parkplatz", 10m, true, 0, AddOnScope.PerPass,
            requireMobileNumber: true));
        var cart = CheckoutFixture.CartWithPass([new CartAddOn(5, "Parkplatz", 10m, CheckoutFixture.SeasonId, "Saison", false)]);

        var result = await fixture.PlaceOrder.HandleAsync(Command(cart));

        var denied = Assert.IsType<PlaceOrder.Result.Denied>(result);
        Assert.Contains("Mobilnummer", denied.Message);
        Assert.False(denied.BackToCart);
        Assert.Empty(fixture.Orders.Stored);
    }

    [Fact]
    public async Task Mobile_number_present_lets_the_order_through()
    {
        var fixture = new CheckoutFixture();
        var cart = CheckoutFixture.CartWithPass([new CartAddOn(5, "Parkplatz", 10m, CheckoutFixture.SeasonId, "Saison", true)]);

        var result = await fixture.PlaceOrder.HandleAsync(Command(cart, phone: "079 000 00 00"));

        Assert.IsType<PlaceOrder.Result.PaymentRequired>(result);
        Assert.Equal(1, fixture.PassReserved);
    }

    [Fact]
    public async Task Exhausted_tier_is_denied_with_the_category_name_and_creates_no_order()
    {
        var fixture = new CheckoutFixture();
        fixture.EventPrices.Usage[CheckoutFixture.EventId] = new CapacityUsage(9, new Dictionary<int, int> { [CheckoutFixture.AdultTier] = 9 });

        var result = await fixture.PlaceOrder.HandleAsync(Command(CheckoutFixture.CartWithTickets(2)));

        var denied = Assert.IsType<PlaceOrder.Result.Denied>(result);
        Assert.Equal("«Erwachsen» ist nicht mehr in dieser Anzahl verfügbar (noch 1).", denied.Message);
        Assert.True(denied.BackToCart);
        Assert.Empty(fixture.Orders.Stored);
        Assert.Equal(0, fixture.EventReserved);
    }

    [Fact]
    public async Task Exhausted_event_quota_is_denied()
    {
        var fixture = new CheckoutFixture();
        fixture.EventPrices.Usage[CheckoutFixture.EventId] = new CapacityUsage(99, new Dictionary<int, int>());

        var result = await fixture.PlaceOrder.HandleAsync(Command(CheckoutFixture.CartWithTickets(2)));

        var denied = Assert.IsType<PlaceOrder.Result.Denied>(result);
        Assert.Contains("nicht mehr genügend Tickets", denied.Message);
    }

    [Fact]
    public async Task Paid_order_requires_payment_and_holds_the_reservation()
    {
        var fixture = new CheckoutFixture();

        var result = await fixture.PlaceOrder.HandleAsync(Command(CheckoutFixture.CartWithTickets(2)));

        var required = Assert.IsType<PlaceOrder.Result.PaymentRequired>(result);
        Assert.Equal("https://pay.test/1", required.PaymentLink);
        var order = fixture.Orders.Stored.Single();
        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Equal("gw-1", order.PayrexxGatewayId);
        Assert.Equal(40m, order.TotalGross);
        Assert.NotNull(OrderSnapshot.Parse(order.FulfillmentPayload));
        Assert.Equal(2, fixture.EventReserved);
        Assert.Equal(2, fixture.TierReserved);
        Assert.Empty(fixture.Tickets.Stored);
        Assert.Contains(fixture.OrderLog.Entries, e => e.OrderId == order.Id && e.Status == OrderStatus.Draft);

        var request = fixture.Payrexx.Requests.Single();
        Assert.Equal(4000, request.AmountInCents);
        Assert.Equal(order.OrderNumber, request.ReferenceId);
        Assert.Equal($"https://tickets.test/checkout/success?t=tok{order.Id}", request.SuccessUrl);
        Assert.Equal($"https://tickets.test/checkout/cancel?t=tok{order.Id}", request.CancelUrl);
        Assert.Equal(request.CancelUrl, request.FailedUrl);
    }

    [Fact]
    public async Task Without_payment_provider_the_order_is_fulfilled_immediately()
    {
        var fixture = new CheckoutFixture();
        fixture.Payrexx.Enabled = false;

        var result = await fixture.PlaceOrder.HandleAsync(Command(CheckoutFixture.CartWithTickets(2), newsletter: true, source: CheckoutSource.Express));

        var completed = Assert.IsType<PlaceOrder.Result.Completed>(result);
        var order = fixture.Orders.Stored.Single(o => o.Id == completed.OrderId);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(2, fixture.Tickets.Stored.Count);
        Assert.All(fixture.Tickets.Stored, t => Assert.Equal(order.Id, t.OrderId));
        Assert.Equal(0, fixture.EventReserved);
        Assert.Equal(0, fixture.TierReserved);
        Assert.Equal(1, fixture.Orders.BillingCopies);
        Assert.Single(fixture.Mailer.Sent);
        Assert.Equal(2, fixture.Mailer.Sent[0].Tickets.Count);
        Assert.Equal(2, fixture.OrderItems.Saved[order.Id].Single().Quantity);
        Assert.Equal(("anna@example.ch", "Anna Muster", "Express"), fixture.Newsletter.Subscriptions.Single());
        Assert.Contains(fixture.OrderLog.Entries, e => e.Status == OrderStatus.Paid);
    }

    [Fact]
    public async Task Free_order_skips_payment_even_with_payrexx_enabled()
    {
        var fixture = new CheckoutFixture();
        fixture.EventPrices.Seed(EventPrice.FromPersistence(1, CheckoutFixture.EventId, null, null,
            [CategoryPrice.FromPersistence(TicketCategory.Adult, 0m, null, null, CheckoutFixture.AdultTier)]));
        var cart = Cart.Empty();
        cart.AddEventTickets(CheckoutFixture.EventId, "Gratis", CheckoutFixture.AdultTier, "Erwachsen", "Erw", 0m, 1);

        var result = await fixture.PlaceOrder.HandleAsync(Command(cart));

        Assert.IsType<PlaceOrder.Result.Completed>(result);
        Assert.Empty(fixture.Payrexx.Requests);
        Assert.Single(fixture.Tickets.Stored);
    }

    [Fact]
    public async Task Gateway_failure_cancels_the_draft_and_releases_the_reservation()
    {
        var fixture = new CheckoutFixture();
        fixture.Payrexx.FailGatewayCreation = true;

        var result = await fixture.PlaceOrder.HandleAsync(Command(CheckoutFixture.CartWithTickets(3)));

        var denied = Assert.IsType<PlaceOrder.Result.Denied>(result);
        Assert.False(denied.BackToCart);
        Assert.Equal(OrderStatus.Cancelled, fixture.Orders.Stored.Single().Status);
        Assert.Equal(0, fixture.EventReserved);
        Assert.Equal(0, fixture.TierReserved);
        Assert.Contains(fixture.OrderLog.Entries, e => e.Status == OrderStatus.Cancelled);
    }

    [Fact]
    public async Task Concurrent_change_is_retried_once()
    {
        var fixture = new CheckoutFixture();
        fixture.EventPrices.FailingSaves = 1;

        var result = await fixture.PlaceOrder.HandleAsync(Command(CheckoutFixture.CartWithTickets(2)));

        Assert.IsType<PlaceOrder.Result.PaymentRequired>(result);
        Assert.Equal(2, fixture.EventReserved);
        Assert.Equal(2, fixture.EventPrices.Saves);
    }

    [Fact]
    public async Task Persistent_contention_is_denied_without_an_order()
    {
        var fixture = new CheckoutFixture();
        fixture.EventPrices.FailingSaves = 2;

        var result = await fixture.PlaceOrder.HandleAsync(Command(CheckoutFixture.CartWithTickets(2)));

        var denied = Assert.IsType<PlaceOrder.Result.Denied>(result);
        Assert.Equal(new CapacityDenied.Contended().Message, denied.Message);
        Assert.Empty(fixture.Orders.Stored);
        Assert.Equal(0, fixture.EventReserved);
    }

    [Fact]
    public async Task Reservations_count_against_later_buyers()
    {
        var fixture = new CheckoutFixture();
        await fixture.PlacedDraftAsync(CheckoutFixture.CartWithTickets(9));

        var result = await fixture.PlaceOrder.HandleAsync(Command(CheckoutFixture.CartWithTickets(2)));

        var denied = Assert.IsType<PlaceOrder.Result.Denied>(result);
        Assert.Equal("«Erwachsen» ist nicht mehr in dieser Anzahl verfügbar (noch 1).", denied.Message);
    }

    [Fact]
    public async Task Conversion_lines_count_against_the_event_total_only()
    {
        var fixture = new CheckoutFixture();
        fixture.EventPrices.Usage[CheckoutFixture.EventId] = new CapacityUsage(0, new Dictionary<int, int> { [CheckoutFixture.AdultTier] = 10 });
        var cart = Cart.Empty();
        cart.AddConversion(CheckoutFixture.EventId, "Match", CheckoutFixture.SeasonId, CheckoutFixture.AdultTier, "Saisonkarte", 0m,
            new ConversionOrigin(TicketType.SeasonPass, Guid.NewGuid(), "Saisonkarte", (int)TicketCategory.Adult, 1));

        var result = await fixture.PlaceOrder.HandleAsync(Command(cart));

        Assert.IsType<PlaceOrder.Result.Completed>(result);
        Assert.Equal(0, fixture.TierReserved);
        var ticket = fixture.Tickets.Stored.Single();
        Assert.Equal(TicketType.SeasonPass, ticket.OriginType);
    }
}
