using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Checkout;
using Xunit;

namespace RedAnts.Ticketing.Tests.Checkout;

public class CanCheckoutTests
{
    private readonly CheckoutFixture _fixture = new();

    private Task<CheckResult> CheckAsync(CheckoutSource source = CheckoutSource.Checkout) =>
        _fixture.CanCheckout.HandleAsync(new CanCheckout.Check(source));

    [Fact]
    public async Task A_cart_with_tickets_may_check_out()
    {
        _fixture.Carts.Save(CheckoutFixture.CartWithTickets());

        Assert.True((await CheckAsync()).IsAllowed);
    }

    [Fact]
    public async Task An_empty_cart_is_denied()
    {
        var denied = Assert.IsType<CheckResult.Denied>(await CheckAsync());

        Assert.IsType<CheckoutDenied.CartEmpty>(denied.Cause);
        Assert.Equal("Der Warenkorb ist leer.", denied.Cause.Message);
    }

    [Fact]
    public async Task A_full_hall_is_denied()
    {
        _fixture.Carts.Save(CheckoutFixture.CartWithTickets());
        _fixture.HallFull();

        var denied = Assert.IsType<CheckResult.Denied>(await CheckAsync());

        Assert.IsType<CheckoutDenied.VenueFull>(denied.Cause);
        Assert.StartsWith("Abendkasse geschlossen", denied.Cause.Message);
    }

    [Fact]
    public async Task A_conversion_only_event_denies_regular_tickets()
    {
        _fixture.Carts.Save(CheckoutFixture.CartWithTickets());
        _fixture.ConversionRules.ConversionOnlyEvents.Add(CheckoutFixture.EventId);

        var denied = Assert.IsType<CheckResult.Denied>(await CheckAsync());

        Assert.IsType<CheckoutDenied.ConversionOnly>(denied.Cause);
        Assert.Contains("nur Kartenumwandlung", denied.Cause.Message);
    }

    [Fact]
    public async Task A_conversion_only_event_still_allows_a_converted_ticket()
    {
        var cart = Cart.Empty();
        cart.AddConversion(CheckoutFixture.EventId, "Match", CheckoutFixture.SeasonId, CheckoutFixture.AdultTier, "Saisonkarte", Money.Of(0m),
            new ConversionOrigin(TicketType.SeasonPass, Guid.NewGuid(), "Saisonkarte", (int)TicketCategory.Adult, 1));
        _fixture.Carts.Save(cart);
        _fixture.ConversionRules.ConversionOnlyEvents.Add(CheckoutFixture.EventId);

        Assert.True((await CheckAsync()).IsAllowed);
    }

    [Fact]
    public async Task A_season_pass_does_not_qualify_for_the_express_checkout()
    {
        _fixture.Carts.Save(CheckoutFixture.CartWithPass());

        var denied = Assert.IsType<CheckResult.Denied>(await CheckAsync(CheckoutSource.Express));

        Assert.IsType<CheckoutDenied.ExpressUnavailable>(denied.Cause);
        Assert.True((await CheckAsync()).IsAllowed);
    }

    [Fact]
    public async Task A_cart_above_the_express_limit_is_denied_for_express()
    {
        _fixture.Carts.Save(CheckoutFixture.CartWithTickets(5));

        var denied = Assert.IsType<CheckResult.Denied>(await CheckAsync(CheckoutSource.Express));

        Assert.IsType<CheckoutDenied.ExpressUnavailable>(denied.Cause);
    }

    [Fact]
    public async Task A_small_cart_qualifies_for_express()
    {
        _fixture.Carts.Save(CheckoutFixture.CartWithTickets(2));

        Assert.True((await CheckAsync(CheckoutSource.Express)).IsAllowed);
    }
}
