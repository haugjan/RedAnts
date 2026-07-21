using RedAnts.Features.Ticketing.Cart;
using Xunit;

namespace RedAnts.Features.Tests.Ticketing;

public class CartTests
{
    private static CartItem EventItem(decimal unitPrice, int qty, int eventId = 1, int tierId = 1) =>
        new() { Kind = CartItemKind.EventTicket, EventId = eventId, TierId = tierId, UnitPrice = unitPrice, Quantity = qty };

    private static CartItem SeasonItem(decimal unitPrice, int seasonId = 1) =>
        new() { Kind = CartItemKind.SeasonPass, SeasonId = seasonId, TierId = 1, UnitPrice = unitPrice, Quantity = 1 };

    private static CartAddOn OrderAddOn(int id, decimal price) => new() { Id = id, Price = price };

    // --- IsEmpty ---

    [Fact]
    public void IsEmpty_NoItemsNoOrderAddOns_ReturnsTrue()
    {
        Assert.True(new Cart().IsEmpty);
    }

    [Fact]
    public void IsEmpty_WithItem_ReturnsFalse()
    {
        var cart = new Cart { Items = [EventItem(10m, 1)] };
        Assert.False(cart.IsEmpty);
    }

    [Fact]
    public void IsEmpty_WithOrderAddOnOnly_ReturnsFalse()
    {
        var cart = new Cart { OrderAddOns = [OrderAddOn(1, 5m)] };
        Assert.False(cart.IsEmpty);
    }

    [Fact]
    public void IsEmpty_WithBothItemsAndOrderAddOns_ReturnsFalse()
    {
        var cart = new Cart
        {
            Items = [EventItem(10m, 1)],
            OrderAddOns = [OrderAddOn(1, 5m)]
        };
        Assert.False(cart.IsEmpty);
    }

    // --- TotalQuantity ---

    [Fact]
    public void TotalQuantity_EmptyCart_ReturnsZero()
    {
        Assert.Equal(0, new Cart().TotalQuantity);
    }

    [Fact]
    public void TotalQuantity_SingleItemQty3_Returns3()
    {
        var cart = new Cart { Items = [EventItem(10m, 3)] };
        Assert.Equal(3, cart.TotalQuantity);
    }

    [Fact]
    public void TotalQuantity_TwoItems_SumsQuantities()
    {
        var cart = new Cart { Items = [EventItem(10m, 3), EventItem(20m, 2)] };
        Assert.Equal(5, cart.TotalQuantity);
    }

    [Fact]
    public void TotalQuantity_EachOrderAddOnCountsAsOne()
    {
        var cart = new Cart { OrderAddOns = [OrderAddOn(1, 5m), OrderAddOn(2, 3m)] };
        Assert.Equal(2, cart.TotalQuantity);
    }

    [Fact]
    public void TotalQuantity_ItemsAndOrderAddOns_SumsBoth()
    {
        var cart = new Cart
        {
            Items = [EventItem(10m, 3), EventItem(20m, 2)],
            OrderAddOns = [OrderAddOn(1, 5m), OrderAddOn(2, 3m)]
        };
        Assert.Equal(7, cart.TotalQuantity);
    }

    // --- TotalAmount ---

    [Fact]
    public void TotalAmount_EmptyCart_ReturnsZero()
    {
        Assert.Equal(0m, new Cart().TotalAmount);
    }

    [Fact]
    public void TotalAmount_SingleItem_ReturnsItsLineTotal()
    {
        var cart = new Cart { Items = [EventItem(20m, 2)] };
        Assert.Equal(40m, cart.TotalAmount);
    }

    [Fact]
    public void TotalAmount_MultipleItems_SumsLineTotals()
    {
        var cart = new Cart { Items = [EventItem(20m, 2), EventItem(15m, 1)] };
        Assert.Equal(55m, cart.TotalAmount);
    }

    [Fact]
    public void TotalAmount_OnlyOrderAddOns_SumsPrices()
    {
        var cart = new Cart { OrderAddOns = [OrderAddOn(1, 5m), OrderAddOn(2, 3m)] };
        Assert.Equal(8m, cart.TotalAmount);
    }

    [Fact]
    public void TotalAmount_ItemsAndOrderAddOns_SumsBoth()
    {
        var cart = new Cart
        {
            Items = [EventItem(20m, 2), EventItem(15m, 1)],
            OrderAddOns = [OrderAddOn(1, 10m)]
        };
        Assert.Equal(65m, cart.TotalAmount);
    }

    [Fact]
    public void TotalAmount_SeasonPassWithAddOns_IncludesAddOnsInLineTotal()
    {
        var item = new CartItem
        {
            Kind = CartItemKind.SeasonPass, SeasonId = 1, TierId = 1,
            UnitPrice = 80m, Quantity = 1,
            AddOns = [new CartAddOn { Id = 1, Price = 20m }]
        };
        var cart = new Cart { Items = [item] };
        Assert.Equal(100m, cart.TotalAmount);
    }
}
