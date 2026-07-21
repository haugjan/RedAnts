using RedAnts.Features.Ticketing.Cart;
using Xunit;

namespace RedAnts.Features.Tests.Ticketing;

public class ExpressCheckoutTests
{
    private static CartItem EventItem(decimal unitPrice, int qty = 1) =>
        new() { Kind = CartItemKind.EventTicket, EventId = 1, TierId = 1, UnitPrice = unitPrice, Quantity = qty };

    private static CartItem SeasonItem(decimal unitPrice) =>
        new() { Kind = CartItemKind.SeasonPass, SeasonId = 1, TierId = 1, UnitPrice = unitPrice, Quantity = 1 };

    [Fact]
    public void MaxAmount_Is50()
    {
        Assert.Equal(50m, ExpressCheckout.MaxAmount);
    }

    [Fact]
    public void IsAllowed_EmptyCart_ReturnsFalse()
    {
        Assert.False(ExpressCheckout.IsAllowed(new Cart()));
    }

    [Fact]
    public void IsAllowed_BelowMax_OnlyEventTickets_ReturnsTrue()
    {
        var cart = new Cart { Items = [EventItem(49.99m)] };
        Assert.True(ExpressCheckout.IsAllowed(cart));
    }

    [Fact]
    public void IsAllowed_TotalExactlyAtMax_ReturnsFalse()
    {
        var cart = new Cart { Items = [EventItem(ExpressCheckout.MaxAmount)] };
        Assert.False(ExpressCheckout.IsAllowed(cart));
    }

    [Fact]
    public void IsAllowed_TotalAboveMax_ReturnsFalse()
    {
        var cart = new Cart { Items = [EventItem(51m)] };
        Assert.False(ExpressCheckout.IsAllowed(cart));
    }

    [Fact]
    public void IsAllowed_SeasonPassAlone_ReturnsFalse()
    {
        var cart = new Cart { Items = [SeasonItem(20m)] };
        Assert.False(ExpressCheckout.IsAllowed(cart));
    }

    [Fact]
    public void IsAllowed_MixOfEventAndSeasonPass_ReturnsFalse()
    {
        var cart = new Cart { Items = [EventItem(10m), SeasonItem(10m)] };
        Assert.False(ExpressCheckout.IsAllowed(cart));
    }

    [Fact]
    public void IsAllowed_MultipleEventTicketsBelowMax_ReturnsTrue()
    {
        var cart = new Cart { Items = [EventItem(10m, 2), EventItem(15m, 1)] };
        Assert.True(ExpressCheckout.IsAllowed(cart));
    }

    [Fact]
    public void IsAllowed_MultipleEventTicketsAtOrAboveMax_ReturnsFalse()
    {
        var cart = new Cart { Items = [EventItem(25m, 2)] };
        Assert.False(ExpressCheckout.IsAllowed(cart));
    }

    [Fact]
    public void IsAllowed_OrderAddOnPushesTotalOverMax_ReturnsFalse()
    {
        var cart = new Cart
        {
            Items = [EventItem(45m)],
            OrderAddOns = [new CartAddOn { Id = 1, Price = 10m }]
        };
        Assert.False(ExpressCheckout.IsAllowed(cart));
    }

    [Fact]
    public void IsAllowed_OnlyOrderAddOnsNoItems_ReturnsTrue()
    {
        // Items is empty → All() is vacuously true; express checkout passes
        var cart = new Cart { OrderAddOns = [new CartAddOn { Id = 1, Price = 5m }] };
        Assert.True(ExpressCheckout.IsAllowed(cart));
    }

    [Fact]
    public void IsAllowed_EventItemBelowMaxPlusOrderAddOnStillBelow_ReturnsTrue()
    {
        var cart = new Cart
        {
            Items = [EventItem(30m)],
            OrderAddOns = [new CartAddOn { Id = 1, Price = 10m }]
        };
        Assert.True(ExpressCheckout.IsAllowed(cart));
    }

    [Fact]
    public void IsAllowed_ItemWithAddOnPushingLineTotalAboveMax_ReturnsFalse()
    {
        var item = new CartItem
        {
            Kind = CartItemKind.EventTicket, EventId = 1, TierId = 1,
            UnitPrice = 30m, Quantity = 1,
            AddOns = [new CartAddOn { Id = 1, Price = 25m }]
        };
        var cart = new Cart { Items = [item] };
        Assert.False(ExpressCheckout.IsAllowed(cart));
    }
}
