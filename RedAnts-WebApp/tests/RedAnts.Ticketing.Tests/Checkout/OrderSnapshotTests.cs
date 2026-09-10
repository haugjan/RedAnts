using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Checkout;
using System.Text.Json;
using Xunit;

namespace RedAnts.Ticketing.Tests.Checkout;

public class OrderSnapshotTests
{
    private static readonly Guid Card = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static Cart FullCart()
    {
        var cart = Cart.Empty();
        cart.AddEventTickets(10, "Spiel A", 3, "Erwachsen", "Erw", Money.Of(25m), 2);
        cart.AddConversion(11, "Spiel B", 7, 4, "Saisonkarte", Money.Of(5m), new ConversionOrigin(TicketType.SeasonSingle, Card, "Flex 42", 2, 1));
        cart.AddSeasonPasses(7, "Saison 26/27", 3, "Erwachsen", "Erw", Money.Of(300m), 2, [new CartAddOn(1, "Parkplatz", Money.Of(20m), 7, "Saison 26/27")]);
        cart.AddOrderAddOns([new CartAddOn(2, "Garderobe", Money.Of(5m), 7, "Saison 26/27")]);
        return cart;
    }

    [Fact]
    public void FromCart_maps_lines_with_origin_and_source()
    {
        var snapshot = OrderSnapshot.FromCart(FullCart(), true, CheckoutSource.Express);

        Assert.Equal(3, snapshot.Items.Count);
        Assert.True(snapshot.SubscribeNewsletter);
        Assert.Equal("Express", snapshot.NewsletterSource);
        Assert.False(snapshot.IsQuickBuy);

        var regular = snapshot.Items[0];
        Assert.Equal((int)CartLineKind.EventTicket, regular.Kind);
        Assert.Equal(10, regular.EventId);
        Assert.Equal(3, regular.TierId);
        Assert.Equal(25m, regular.UnitPrice);
        Assert.Equal(2, regular.Quantity);
        Assert.Equal("Spiel A", regular.EventName);
        Assert.Equal("Erwachsen", regular.CategoryName);
        Assert.Null(regular.OriginType);
        Assert.Null(regular.OriginCardUuid);
        Assert.False(regular.IsConversion);
        Assert.False(regular.IsSeasonPass);

        var conversion = snapshot.Items[1];
        Assert.Equal((int)TicketType.SeasonSingle, conversion.OriginType);
        Assert.Equal(Card.ToString(), conversion.OriginCardUuid);
        Assert.Equal(2, conversion.OriginCategory);
        Assert.Equal(7, conversion.SeasonId);
        Assert.True(conversion.IsConversion);

        var pass = snapshot.Items[2];
        Assert.Equal((int)CartLineKind.SeasonPass, pass.Kind);
        Assert.Equal(7, pass.SeasonId);
        Assert.True(pass.IsSeasonPass);
    }

    [Fact]
    public void FromCart_maps_pass_add_ons_with_line_quantity_and_order_add_ons_with_one()
    {
        var snapshot = OrderSnapshot.FromCart(FullCart(), false, CheckoutSource.Checkout);

        Assert.Equal(2, snapshot.AddOns.Count);

        var perPass = snapshot.AddOns[0];
        Assert.Equal(1, perPass.Id);
        Assert.Equal(7, perPass.SeasonId);
        Assert.Equal("Saison 26/27", perPass.EventName);
        Assert.Equal(3, perPass.TierId);
        Assert.Equal("Erwachsen", perPass.CategoryName);
        Assert.Equal("Parkplatz", perPass.Label);
        Assert.Equal(20m, perPass.Price);
        Assert.Equal(2, perPass.Quantity);

        var perOrder = snapshot.AddOns[1];
        Assert.Equal(2, perOrder.Id);
        Assert.Equal(0, perOrder.TierId);
        Assert.Equal("", perOrder.CategoryName);
        Assert.Equal("Saison 26/27", perOrder.EventName);
        Assert.Equal(1, perOrder.Quantity);
    }

    [Fact]
    public void QuickBuy_source_is_recognised()
    {
        var snapshot = OrderSnapshot.FromCart(FullCart(), false, CheckoutSource.QuickBuy);

        Assert.Equal("QuickBuy", snapshot.NewsletterSource);
        Assert.True(snapshot.IsQuickBuy);
    }

    [Fact]
    public void Demands_are_grouped_by_event_and_season()
    {
        var snapshot = OrderSnapshot.FromCart(FullCart(), false, CheckoutSource.Checkout);

        Assert.Equal([10, 11], snapshot.EventIds);
        Assert.Equal([7], snapshot.SeasonIds);
        Assert.Equal([new TierDemand(3, 2)], snapshot.EventDemand(10));
        Assert.Equal([new TierDemand(4, 1, true)], snapshot.EventDemand(11));
        Assert.Equal([new TierDemand(3, 2)], snapshot.PassDemand(7));
        Assert.Empty(snapshot.EventDemand(7));
        Assert.Empty(snapshot.PassDemand(10));
    }

    [Fact]
    public void Serialisation_uses_the_legacy_property_names()
    {
        var snapshot = OrderSnapshot.FromCart(FullCart(), true, CheckoutSource.Checkout);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(snapshot));
        var root = document.RootElement;

        Assert.Equal(["Items", "AddOns", "SubscribeNewsletter", "NewsletterSource"], root.EnumerateObject().Select(p => p.Name).ToArray());
        Assert.Equal(
            ["Kind", "EventId", "SeasonId", "TierId", "UnitPrice", "Quantity", "EventName", "CategoryName", "OriginType", "OriginCardUuid", "OriginCategory"],
            root.GetProperty("Items")[0].EnumerateObject().Select(p => p.Name).ToArray());
        Assert.Equal(
            ["Id", "SeasonId", "EventName", "TierId", "CategoryName", "Label", "Price", "Quantity"],
            root.GetProperty("AddOns")[0].EnumerateObject().Select(p => p.Name).ToArray());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Items")[0].GetProperty("OriginType").ValueKind);
        Assert.Equal(Card.ToString(), root.GetProperty("Items")[1].GetProperty("OriginCardUuid").GetString());
    }

    [Fact]
    public void Legacy_payload_deserialises()
    {
        const string legacy = """
            {"Items":[{"Kind":0,"EventId":10,"SeasonId":0,"TierId":3,"UnitPrice":25.0,"Quantity":2,"EventName":"Spiel A","CategoryName":"Erwachsen","OriginType":null,"OriginCardUuid":null,"OriginCategory":0},
                      {"Kind":1,"EventId":0,"SeasonId":7,"TierId":3,"UnitPrice":300.0,"Quantity":1,"EventName":"Saison 26/27","CategoryName":"Erwachsen"}],
             "AddOns":[{"Id":1,"SeasonId":7,"EventName":"Saison 26/27","TierId":3,"CategoryName":"Erwachsen","Label":"Parkplatz","Price":20.0,"Quantity":1}],
             "SubscribeNewsletter":true,"NewsletterSource":"Kasse"}
            """;

        var snapshot = JsonSerializer.Deserialize<OrderSnapshot>(legacy);

        Assert.NotNull(snapshot);
        Assert.Equal(2, snapshot.Items.Count);
        Assert.Equal(25m, snapshot.Items[0].UnitPrice);
        Assert.True(snapshot.Items[1].IsSeasonPass);
        Assert.Null(snapshot.Items[1].OriginType);
        Assert.Equal("Parkplatz", Assert.Single(snapshot.AddOns).Label);
        Assert.True(snapshot.SubscribeNewsletter);
        Assert.Equal("Kasse", snapshot.NewsletterSource);
        Assert.False(snapshot.IsQuickBuy);
        Assert.Equal([new TierDemand(3, 2)], snapshot.EventDemand(10));
        Assert.Equal([new TierDemand(3, 1)], snapshot.PassDemand(7));
    }

    [Fact]
    public void Round_trip_keeps_conversion_fields()
    {
        var original = OrderSnapshot.FromCart(FullCart(), false, CheckoutSource.QuickBuy);

        var restored = JsonSerializer.Deserialize<OrderSnapshot>(JsonSerializer.Serialize(original));

        Assert.NotNull(restored);
        Assert.Equal(original, restored with { Items = original.Items, AddOns = original.AddOns });
        Assert.Equal(original.Items, restored.Items);
        Assert.Equal(original.AddOns, restored.AddOns);
    }
}
