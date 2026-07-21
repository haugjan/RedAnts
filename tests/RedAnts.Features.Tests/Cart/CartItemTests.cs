using RedAnts.Features.Ticketing.Cart;
using Xunit;

namespace RedAnts.Features.Tests.Ticketing;

public class CartItemTests
{
    private static CartAddOn AddOn(int id, decimal price) => new() { Id = id, Price = price };

    // --- RefId ---

    [Fact]
    public void RefId_EventTicket_ReturnsEventId()
    {
        var item = new CartItem { Kind = CartItemKind.EventTicket, EventId = 10, SeasonId = 5 };
        Assert.Equal(10, item.RefId);
    }

    [Fact]
    public void RefId_SeasonPass_ReturnsSeasonId()
    {
        var item = new CartItem { Kind = CartItemKind.SeasonPass, EventId = 10, SeasonId = 5 };
        Assert.Equal(5, item.RefId);
    }

    // --- AddOnKey ---

    [Fact]
    public void AddOnKey_NoAddOns_ReturnsEmpty()
    {
        Assert.Equal("", new CartItem().AddOnKey);
    }

    [Fact]
    public void AddOnKey_SingleAddOn_ReturnsItsId()
    {
        var item = new CartItem { AddOns = [AddOn(3, 0m)] };
        Assert.Equal("3", item.AddOnKey);
    }

    [Fact]
    public void AddOnKey_MultipleAddOns_SortedAscendingJoinedByDash()
    {
        var item = new CartItem { AddOns = [AddOn(8, 0m), AddOn(2, 0m), AddOn(5, 0m)] };
        Assert.Equal("2-5-8", item.AddOnKey);
    }

    [Fact]
    public void AddOnKey_AlreadySorted_RemainsUnchanged()
    {
        var item = new CartItem { AddOns = [AddOn(1, 0m), AddOn(4, 0m)] };
        Assert.Equal("1-4", item.AddOnKey);
    }

    // --- Key ---

    [Fact]
    public void Key_EventTicket_NoAddOns_EncodesKindRefIdTierEmpty()
    {
        var item = new CartItem { Kind = CartItemKind.EventTicket, EventId = 10, TierId = 2 };
        Assert.Equal("0:10:2:", item.Key);
    }

    [Fact]
    public void Key_SeasonPass_UsesSeasonIdNotEventId()
    {
        var item = new CartItem { Kind = CartItemKind.SeasonPass, EventId = 99, SeasonId = 5, TierId = 1 };
        Assert.Equal("1:5:1:", item.Key);
    }

    [Fact]
    public void Key_WithAddOns_AppendsSortedAddOnKey()
    {
        var item = new CartItem
        {
            Kind = CartItemKind.SeasonPass, SeasonId = 5, TierId = 1,
            AddOns = [AddOn(7, 0m), AddOn(3, 0m)]
        };
        Assert.Equal("1:5:1:3-7", item.Key);
    }

    [Fact]
    public void Key_TwoDifferentItems_HaveDistinctKeys()
    {
        var item1 = new CartItem { Kind = CartItemKind.EventTicket, EventId = 1, TierId = 1 };
        var item2 = new CartItem { Kind = CartItemKind.EventTicket, EventId = 2, TierId = 1 };
        Assert.NotEqual(item1.Key, item2.Key);
    }

    [Fact]
    public void Key_SameParamsDifferentTier_HaveDistinctKeys()
    {
        var item1 = new CartItem { Kind = CartItemKind.EventTicket, EventId = 1, TierId = 1 };
        var item2 = new CartItem { Kind = CartItemKind.EventTicket, EventId = 1, TierId = 2 };
        Assert.NotEqual(item1.Key, item2.Key);
    }

    // --- AddOnTotal ---

    [Fact]
    public void AddOnTotal_NoAddOns_ReturnsZero()
    {
        Assert.Equal(0m, new CartItem().AddOnTotal);
    }

    [Fact]
    public void AddOnTotal_SingleAddOn_ReturnsItsPrice()
    {
        var item = new CartItem { AddOns = [AddOn(1, 12.50m)] };
        Assert.Equal(12.50m, item.AddOnTotal);
    }

    [Fact]
    public void AddOnTotal_MultipleAddOns_SumsAllPrices()
    {
        var item = new CartItem { AddOns = [AddOn(1, 10m), AddOn(2, 5m), AddOn(3, 3m)] };
        Assert.Equal(18m, item.AddOnTotal);
    }

    // --- LineTotal ---

    [Fact]
    public void LineTotal_NoAddOnsQuantityOne_ReturnsUnitPrice()
    {
        var item = new CartItem { UnitPrice = 20m, Quantity = 1 };
        Assert.Equal(20m, item.LineTotal);
    }

    [Fact]
    public void LineTotal_NoAddOns_IsUnitPriceTimesQuantity()
    {
        var item = new CartItem { UnitPrice = 20m, Quantity = 2 };
        Assert.Equal(40m, item.LineTotal);
    }

    [Fact]
    public void LineTotal_WithAddOns_IncludesAddOnInPerUnitCost()
    {
        var item = new CartItem { UnitPrice = 20m, Quantity = 3, AddOns = [AddOn(1, 5m)] };
        Assert.Equal(75m, item.LineTotal);
    }

    [Fact]
    public void LineTotal_MultipleAddOns_AllIncludedPerUnit()
    {
        var item = new CartItem
        {
            UnitPrice = 10m,
            Quantity = 2,
            AddOns = [AddOn(1, 3m), AddOn(2, 2m)]
        };
        Assert.Equal(30m, item.LineTotal);
    }

    [Fact]
    public void LineTotal_ZeroQuantity_ReturnsZero()
    {
        var item = new CartItem { UnitPrice = 20m, Quantity = 0, AddOns = [AddOn(1, 5m)] };
        Assert.Equal(0m, item.LineTotal);
    }

    [Fact]
    public void LineTotal_ZeroUnitPrice_OnlyAddOnsCharged()
    {
        var item = new CartItem { UnitPrice = 0m, Quantity = 3, AddOns = [AddOn(1, 5m)] };
        Assert.Equal(15m, item.LineTotal);
    }
}
