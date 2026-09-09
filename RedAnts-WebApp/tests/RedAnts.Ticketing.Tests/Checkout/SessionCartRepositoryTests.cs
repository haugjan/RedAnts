using Microsoft.AspNetCore.DataProtection;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Checkout.Infrastructure;
using System.Text.Json;
using Xunit;

namespace RedAnts.Ticketing.Tests.Checkout;

public class SessionCartRepositoryTests
{
    private const string LegacyJson = """
        {"Items":[
          {"Kind":0,"EventId":12,"SeasonId":0,"EventName":"Red Ants vs. Zug","TierId":3,"CategoryName":"Erwachsene","StandardCategoryName":"Erw.","UnitPrice":25.0,"Quantity":2,"AddOns":[],"OriginType":null,"OriginCardUuid":null,"OriginLabel":null,"OriginCategory":0,"OriginCap":0},
          {"Kind":0,"EventId":12,"SeasonId":7,"EventName":"Red Ants vs. Zug","TierId":3,"CategoryName":"Umwandlung","StandardCategoryName":"Umwandlung","UnitPrice":5.0,"Quantity":1,"AddOns":[],"OriginType":2,"OriginCardUuid":"5b1f6f0e-9c1a-4d2e-8f6a-0c9b7d1e2a33","OriginLabel":"Saisonkarte 0815","OriginCategory":1,"OriginCap":1},
          {"Kind":1,"EventId":0,"SeasonId":7,"EventName":"Saison 2026/27","TierId":9,"CategoryName":"Erwachsene","StandardCategoryName":"Erw.","UnitPrice":300.0,"Quantity":1,"AddOns":[{"Id":4,"Label":"Parkplatz","Price":40.0,"SeasonId":7,"SeasonName":"Saison 2026/27"}],"OriginType":null,"OriginCardUuid":null,"OriginLabel":null,"OriginCategory":0,"OriginCap":0}
        ],
        "OrderAddOns":[{"Id":5,"Label":"Garderobe","Price":20.0,"SeasonId":7,"SeasonName":"Saison 2026/27"}]}
        """;

    [Fact]
    public void Legacy_session_json_maps_to_the_domain_cart()
    {
        var cart = CartJson.Read(LegacyJson);

        Assert.Equal(3, cart.Items.Count);
        var regular = cart.Items[0];
        Assert.Equal(CartLineKind.EventTicket, regular.Kind);
        Assert.Equal(12, regular.EventId);
        Assert.Equal(2, regular.Quantity);
        Assert.False(regular.IsConversion);

        var conversion = cart.Items[1];
        Assert.True(conversion.IsConversion);
        Assert.Equal(TicketType.SeasonPass, conversion.Origin!.CardType);
        Assert.Equal(Guid.Parse("5b1f6f0e-9c1a-4d2e-8f6a-0c9b7d1e2a33"), conversion.Origin.CardUuid);
        Assert.Equal("Saisonkarte 0815", conversion.OriginLabel);
        Assert.Equal(1, conversion.OriginCap);

        var pass = cart.Items[2];
        Assert.Equal(CartLineKind.SeasonPass, pass.Kind);
        Assert.Single(pass.AddOns);
        Assert.Equal("Parkplatz", pass.AddOns[0].Label);
        Assert.False(pass.AddOns[0].RequiresMobileNumber);

        Assert.Single(cart.OrderAddOns);
        Assert.Equal("Garderobe", cart.OrderAddOns[0].Label);
        Assert.Equal(25m * 2 + 5m + 340m + 20m, cart.TotalAmount);
    }

    [Fact]
    public void Written_json_keeps_the_legacy_property_names_and_round_trips()
    {
        var cart = CartJson.Read(LegacyJson);
        cart.AddOrderAddOns([new CartAddOn(6, "Mitgliederkarte", 0m, 7, "Saison 2026/27", RequiresMobileNumber: true)]);

        var json = CartJson.Write(cart);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var firstItem = root.GetProperty("Items")[0];
        foreach (var name in new[] { "Kind", "EventId", "SeasonId", "EventName", "TierId", "CategoryName", "StandardCategoryName", "UnitPrice", "Quantity", "AddOns", "OriginType", "OriginCardUuid", "OriginLabel", "OriginCategory", "OriginCap" })
            Assert.True(firstItem.TryGetProperty(name, out _), $"missing {name}");
        Assert.Equal(2, root.GetProperty("OrderAddOns").GetArrayLength());

        var again = CartJson.Read(json);
        Assert.Equal(cart.Items.Select(i => i.Key), again.Items.Select(i => i.Key));
        Assert.Equal(cart.TotalAmount, again.TotalAmount);
        Assert.True(again.RequiresMobileNumber);
        Assert.Equal(again.Items[1].Origin, cart.Items[1].Origin);
    }

    [Fact]
    public void Empty_or_missing_json_yields_an_empty_cart()
    {
        Assert.True(CartJson.Read(null).IsEmpty);
        Assert.True(CartJson.Read("").IsEmpty);
    }

    [Fact]
    public void Order_tokens_round_trip_and_reject_garbage()
    {
        var tokens = new DataProtectionOrderTokens(new EphemeralDataProtectionProvider());

        var token = tokens.Protect(4711);

        Assert.Equal(4711, tokens.Unprotect(token));
        Assert.Null(tokens.Unprotect(token + "x"));
        Assert.Null(tokens.Unprotect(null));
        Assert.Null(tokens.Unprotect(""));
    }
}
