using RedAnts.Domain.Ticketing.Sales;
using Xunit;

namespace RedAnts.Ticketing.Tests.Sales;

public class CartTests
{
    private static readonly Guid CardA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CardB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static CartAddOn Parking(int seasonId = 7, bool mobile = false) =>
        new(1, "Parkplatz", 20m, seasonId, "Saison 26/27", mobile);

    private static CartAddOn Locker(int seasonId = 7) =>
        new(2, "Garderobe", 5m, seasonId, "Saison 26/27");

    private static ConversionOrigin Origin(Guid card, int cap = 3) =>
        new(TicketType.SeasonPass, card, "Saisonkarte 123", 0, cap);

    [Fact]
    public void AddEventTickets_merges_same_event_and_tier_and_refreshes_name_and_price()
    {
        var cart = Cart.Empty();
        cart.AddEventTickets(10, "Spiel A", 3, "Erwachsen", "Erw.", 25m, 2);
        cart.AddEventTickets(10, "Spiel A (neu)", 3, "Erwachsene", "Erw", 30m, 1);

        var line = Assert.Single(cart.Items);
        Assert.Equal(3, line.Quantity);
        Assert.Equal("Spiel A (neu)", line.EventName);
        Assert.Equal("Erwachsene", line.CategoryName);
        Assert.Equal("Erw", line.StandardCategoryName);
        Assert.Equal(30m, line.UnitPrice);
        Assert.Equal(CartLineKind.EventTicket, line.Kind);
        Assert.Equal(10, line.RefId);
    }

    [Fact]
    public void AddEventTickets_keeps_different_tiers_apart()
    {
        var cart = Cart.Empty();
        cart.AddEventTickets(10, "Spiel A", 3, "Erwachsen", "Erw", 25m, 1);
        cart.AddEventTickets(10, "Spiel A", 4, "Jugend", "Jug", 15m, 1);

        Assert.Equal(2, cart.Items.Count);
    }

    [Fact]
    public void AddEventTickets_caps_at_max_quantity_per_line()
    {
        var cart = Cart.Empty();
        cart.AddEventTickets(10, "Spiel A", 3, "Erwachsen", "Erw", 25m, 40);
        cart.AddEventTickets(10, "Spiel A", 3, "Erwachsen", "Erw", 25m, 40);

        Assert.Equal(Cart.MaxQuantityPerLine, Assert.Single(cart.Items).Quantity);

        var fresh = Cart.Empty();
        fresh.AddEventTickets(10, "Spiel A", 3, "Erwachsen", "Erw", 25m, 999);
        Assert.Equal(50, Assert.Single(fresh.Items).Quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddEventTickets_rejects_non_positive_quantity(int quantity)
    {
        var cart = Cart.Empty();
        Assert.Throws<DomainException>(() => cart.AddEventTickets(10, "Spiel A", 3, "Erwachsen", "Erw", 25m, quantity));
        Assert.True(cart.IsEmpty);
    }

    [Fact]
    public void AddSeasonPasses_merges_by_season_tier_and_add_on_set_and_replaces_add_ons()
    {
        var cart = Cart.Empty();
        cart.AddSeasonPasses(7, "Saison", 3, "Erwachsen", "Erw", 300m, 1, [Parking()]);
        cart.AddSeasonPasses(7, "Saison 26/27", 3, "Erwachsene", "Erw.", 320m, 2, [Parking(mobile: true)]);

        var line = Assert.Single(cart.Items);
        Assert.Equal(CartLineKind.SeasonPass, line.Kind);
        Assert.Equal(3, line.Quantity);
        Assert.Equal("Saison 26/27", line.EventName);
        Assert.Equal(320m, line.UnitPrice);
        Assert.True(Assert.Single(line.AddOns).RequiresMobileNumber);
        Assert.Equal(7, line.RefId);
    }

    [Fact]
    public void AddSeasonPasses_with_different_add_on_sets_creates_separate_lines()
    {
        var cart = Cart.Empty();
        cart.AddSeasonPasses(7, "Saison", 3, "Erwachsen", "Erw", 300m, 1, [Parking()]);
        cart.AddSeasonPasses(7, "Saison", 3, "Erwachsen", "Erw", 300m, 1, []);
        cart.AddSeasonPasses(7, "Saison", 3, "Erwachsen", "Erw", 300m, 1, [Locker(), Parking()]);

        Assert.Equal(3, cart.Items.Count);
        Assert.Equal("1-2", cart.Items[2].AddOnKey);
        Assert.Equal("1:7:3:1-2", cart.Items[2].Key);
    }

    [Fact]
    public void AddSeasonPasses_rejects_non_positive_quantity() =>
        Assert.Throws<DomainException>(() => Cart.Empty().AddSeasonPasses(7, "Saison", 3, "Erwachsen", "Erw", 300m, 0, []));

    [Fact]
    public void AddConversion_adds_one_per_call_up_to_the_cap()
    {
        var cart = Cart.Empty();
        var origin = Origin(CardA, cap: 2);

        Assert.Equal(1, cart.AddConversion(10, "Spiel A", 7, 3, "Erwachsen", 0m, origin));
        Assert.Equal(1, cart.AddConversion(10, "Spiel A", 7, 3, "Erwachsen", 0m, origin));
        Assert.Equal(0, cart.AddConversion(10, "Spiel A", 7, 3, "Erwachsen", 0m, origin));

        var line = Assert.Single(cart.Items);
        Assert.Equal(2, line.Quantity);
        Assert.True(line.IsConversion);
        Assert.Equal(2, line.OriginCap);
        Assert.Equal("Saisonkarte 123", line.OriginLabel);
        Assert.Equal(7, line.SeasonId);
    }

    [Fact]
    public void AddConversion_caps_the_origin_at_max_quantity_per_line()
    {
        var cart = Cart.Empty();
        cart.AddConversion(10, "Spiel A", 7, 3, "Erwachsen", 0m, Origin(CardA, cap: 500));

        Assert.Equal(Cart.MaxQuantityPerLine, Assert.Single(cart.Items).OriginCap);
    }

    [Fact]
    public void AddConversion_never_merges_with_regular_lines_of_the_same_tier()
    {
        var cart = Cart.Empty();
        cart.AddEventTickets(10, "Spiel A", 3, "Erwachsen", "Erw", 25m, 1);
        cart.AddConversion(10, "Spiel A", 7, 3, "Erwachsen", 0m, Origin(CardA));
        cart.AddConversion(10, "Spiel A", 7, 3, "Erwachsen", 0m, Origin(CardB));
        cart.AddEventTickets(10, "Spiel A", 3, "Erwachsen", "Erw", 25m, 1);

        Assert.Equal(3, cart.Items.Count);
        Assert.Equal(2, cart.Items[0].Quantity);
        Assert.False(cart.Items[0].IsConversion);
        Assert.Equal(1, cart.Items[1].Quantity);
        Assert.Equal(1, cart.Items[2].Quantity);
    }

    [Fact]
    public void Conversion_line_key_includes_the_card_uuid()
    {
        var cart = Cart.Empty();
        cart.AddConversion(10, "Spiel A", 7, 3, "Erwachsen", 0m, Origin(CardA));

        Assert.Equal($"0:10:3::{CardA}", Assert.Single(cart.Items).Key);
    }

    [Fact]
    public void AddOrderAddOns_ignores_duplicates()
    {
        var cart = Cart.Empty();
        cart.AddOrderAddOns([Parking(), Parking(), Locker()]);
        cart.AddOrderAddOns([Locker()]);

        Assert.Equal(2, cart.OrderAddOns.Count);
    }

    [Fact]
    public void RemoveOrderAddOn_removes_by_id()
    {
        var cart = Cart.Empty();
        cart.AddOrderAddOns([Parking(), Locker()]);
        cart.RemoveOrderAddOn(1);

        Assert.Equal(2, Assert.Single(cart.OrderAddOns).Id);
    }

    [Fact]
    public void SetQuantity_caps_at_max_and_removes_at_zero()
    {
        var cart = Cart.Empty();
        cart.AddEventTickets(10, "Spiel A", 3, "Erwachsen", "Erw", 25m, 1);
        var key = cart.Items[0].Key;

        cart.SetQuantity(key, 80);
        Assert.Equal(50, cart.Items[0].Quantity);

        cart.SetQuantity(key, 0);
        Assert.Empty(cart.Items);
    }

    [Fact]
    public void SetQuantity_caps_conversion_lines_at_their_origin_cap()
    {
        var cart = Cart.Empty();
        cart.AddConversion(10, "Spiel A", 7, 3, "Erwachsen", 0m, Origin(CardA, cap: 3));
        var key = cart.Items[0].Key;

        cart.SetQuantity(key, 10);
        Assert.Equal(3, cart.Items[0].Quantity);
    }

    [Fact]
    public void SetQuantity_ignores_unknown_keys()
    {
        var cart = Cart.Empty();
        cart.AddEventTickets(10, "Spiel A", 3, "Erwachsen", "Erw", 25m, 2);
        cart.SetQuantity("nope", 5);

        Assert.Equal(2, Assert.Single(cart.Items).Quantity);
    }

    [Fact]
    public void Removing_the_last_pass_of_a_season_prunes_its_order_add_ons()
    {
        var cart = Cart.Empty();
        cart.AddSeasonPasses(7, "Saison", 3, "Erwachsen", "Erw", 300m, 1, []);
        cart.AddSeasonPasses(8, "Saison alt", 3, "Erwachsen", "Erw", 300m, 1, []);
        cart.AddOrderAddOns([Parking(7), Locker(8)]);

        cart.Remove(cart.Items[0].Key);

        Assert.Equal(8, Assert.Single(cart.OrderAddOns).SeasonId);

        cart.SetQuantity(cart.Items[0].Key, 0);
        Assert.Empty(cart.OrderAddOns);
    }

    [Fact]
    public void Clear_empties_lines_and_order_add_ons()
    {
        var cart = Cart.Empty();
        cart.AddSeasonPasses(7, "Saison", 3, "Erwachsen", "Erw", 300m, 1, []);
        cart.AddOrderAddOns([Parking()]);

        cart.Clear();

        Assert.True(cart.IsEmpty);
        Assert.Empty(cart.Items);
        Assert.Empty(cart.OrderAddOns);
    }

    [Fact]
    public void Totals_include_line_add_ons_and_order_add_ons()
    {
        var cart = Cart.Empty();
        cart.AddEventTickets(10, "Spiel A", 3, "Erwachsen", "Erw", 25m, 2);
        cart.AddSeasonPasses(7, "Saison", 3, "Erwachsen", "Erw", 300m, 2, [Parking()]);
        cart.AddOrderAddOns([Locker()]);

        Assert.Equal(5, cart.TotalQuantity);
        Assert.Equal(50m + 2 * 320m + 5m, cart.TotalAmount);
        Assert.Equal(640m, cart.Items[1].LineTotal);
        Assert.Equal(20m, cart.Items[1].AddOnTotal);
    }

    [Fact]
    public void IsEmpty_is_false_with_only_order_add_ons()
    {
        var cart = Cart.Empty();
        cart.AddOrderAddOns([Parking()]);

        Assert.False(cart.IsEmpty);
        Assert.Equal(1, cart.TotalQuantity);
    }

    [Fact]
    public void QualifiesForExpress_needs_a_small_pass_free_cart()
    {
        Assert.False(Cart.Empty().QualifiesForExpress);

        var small = Cart.Empty();
        small.AddEventTickets(10, "Spiel A", 3, "Erwachsen", "Erw", 24.99m, 2);
        Assert.True(small.QualifiesForExpress);

        var atLimit = Cart.Empty();
        atLimit.AddEventTickets(10, "Spiel A", 3, "Erwachsen", "Erw", 25m, 2);
        Assert.False(atLimit.QualifiesForExpress);

        var withPass = Cart.Empty();
        withPass.AddSeasonPasses(7, "Saison", 3, "Erwachsen", "Erw", 10m, 1, []);
        Assert.False(withPass.QualifiesForExpress);
    }

    [Fact]
    public void RequiresMobileNumber_comes_from_line_or_order_add_ons()
    {
        var none = Cart.Empty();
        none.AddSeasonPasses(7, "Saison", 3, "Erwachsen", "Erw", 300m, 1, [Locker()]);
        Assert.False(none.RequiresMobileNumber);

        var viaLine = Cart.Empty();
        viaLine.AddSeasonPasses(7, "Saison", 3, "Erwachsen", "Erw", 300m, 1, [Parking(mobile: true)]);
        Assert.True(viaLine.RequiresMobileNumber);

        var viaOrder = Cart.Empty();
        viaOrder.AddSeasonPasses(7, "Saison", 3, "Erwachsen", "Erw", 300m, 1, []);
        viaOrder.AddOrderAddOns([Parking(mobile: true)]);
        Assert.True(viaOrder.RequiresMobileNumber);
    }

    [Fact]
    public void EventIds_and_SeasonIds_are_distinct_per_kind()
    {
        var cart = Cart.Empty();
        cart.AddEventTickets(10, "Spiel A", 3, "Erwachsen", "Erw", 25m, 1);
        cart.AddEventTickets(10, "Spiel A", 4, "Jugend", "Jug", 15m, 1);
        cart.AddConversion(11, "Spiel B", 7, 3, "Erwachsen", 0m, Origin(CardA));
        cart.AddSeasonPasses(7, "Saison", 3, "Erwachsen", "Erw", 300m, 1, []);
        cart.AddSeasonPasses(7, "Saison", 4, "Jugend", "Jug", 200m, 1, []);

        Assert.Equal([10, 11], cart.EventIds);
        Assert.Equal([7], cart.SeasonIds);
    }

    [Fact]
    public void HasRegularTicketsFor_ignores_conversions()
    {
        var cart = Cart.Empty();
        cart.AddConversion(10, "Spiel A", 7, 3, "Erwachsen", 0m, Origin(CardA));
        Assert.False(cart.HasRegularTicketsFor(10));

        cart.AddEventTickets(10, "Spiel A", 3, "Erwachsen", "Erw", 25m, 1);
        Assert.True(cart.HasRegularTicketsFor(10));
        Assert.False(cart.HasRegularTicketsFor(99));
    }

    [Fact]
    public void EventDemand_lists_tier_quantities_and_conversion_flags_per_event()
    {
        var cart = Cart.Empty();
        cart.AddEventTickets(10, "Spiel A", 3, "Erwachsen", "Erw", 25m, 2);
        cart.AddConversion(10, "Spiel A", 7, 3, "Erwachsen", 0m, Origin(CardA));
        cart.AddEventTickets(11, "Spiel B", 4, "Jugend", "Jug", 15m, 1);

        var demand = cart.EventDemand(10);

        Assert.Equal(2, demand.Count);
        Assert.Contains(new TierDemand(3, 2), demand);
        Assert.Contains(new TierDemand(3, 1, true), demand);
        Assert.Empty(cart.EventDemand(99));
    }

    [Fact]
    public void PassDemand_lists_pass_lines_of_the_season()
    {
        var cart = Cart.Empty();
        cart.AddSeasonPasses(7, "Saison", 3, "Erwachsen", "Erw", 300m, 2, [Parking()]);
        cart.AddSeasonPasses(7, "Saison", 3, "Erwachsen", "Erw", 300m, 1, []);
        cart.AddEventTickets(10, "Spiel A", 3, "Erwachsen", "Erw", 25m, 5);

        var demand = cart.PassDemand(7);

        Assert.Equal(2, demand.Count);
        Assert.All(demand, d => Assert.Equal(3, d.TierId));
        Assert.All(demand, d => Assert.False(d.IsConversion));
        Assert.Equal(3, demand.Sum(d => d.Quantity));
    }

    [Fact]
    public void FromPersistence_keeps_lines_and_floors_quantity_at_one()
    {
        var line = CartLine.FromPersistence(CartLineKind.EventTicket, 10, 0, "Spiel A", 3, "Erwachsen", "Erw", 25m, 0);
        var conversion = CartLine.FromPersistence(CartLineKind.EventTicket, 10, 7, "Spiel A", 3, "Erwachsen", "Erw", 0m, 2, null, Origin(CardA));
        var cart = Cart.FromPersistence([line, conversion], [Parking()]);

        Assert.Equal(2, cart.Items.Count);
        Assert.Equal(1, cart.Items[0].Quantity);
        Assert.Empty(cart.Items[0].AddOns);
        Assert.True(cart.Items[1].IsConversion);
        Assert.Equal(1, cart.OrderAddOns.Count);
        Assert.Equal(1, cart.EventDemand(10).Count(d => d.IsConversion));
    }

    [Fact]
    public void FromPersistence_tolerates_null_texts()
    {
        var line = CartLine.FromPersistence(CartLineKind.SeasonPass, 0, 7, null!, 3, null!, null!, 300m, 1);

        Assert.Equal("", line.EventName);
        Assert.Equal("", line.CategoryName);
        Assert.Equal("", line.StandardCategoryName);
    }
}
