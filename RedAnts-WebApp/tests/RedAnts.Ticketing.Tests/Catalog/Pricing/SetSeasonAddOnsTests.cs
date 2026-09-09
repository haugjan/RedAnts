using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Catalog;
using RedAnts.Ticketing.Features.Catalog.Pricing;
using RedAnts.Ticketing.Tests.Catalog;
using Xunit;

namespace RedAnts.Ticketing.Tests.Catalog.Pricing;

public class SetSeasonAddOnsTests
{
    private const int SeasonId = 3;

    private static SeasonAddOnInput Input(string label, IReadOnlyList<int>? tierIds = null, decimal price = 10m, AddOnScope scope = AddOnScope.PerPass,
        bool promoOnly = false, bool requireMobile = false, string? longTitle = null, string? before = null, string? after = null, bool active = true) =>
        new(label, longTitle, price, active, scope, before, after, tierIds ?? [], promoOnly, requireMobile);

    [Fact]
    public async Task Unknown_and_promo_tier_ids_are_filtered_and_blank_labels_dropped()
    {
        var tiers = new InMemoryPriceTiers();
        tiers.Seed(7, SeasonId, "Erwachsen");
        tiers.Seed(70, SeasonId, "Frühbucher", promoOfTierId: 7);
        tiers.Seed(8, SeasonId, "Kind", sortOrder: 1);
        var addOns = new RecordingSeasonAddOns();

        await new SetSeasonAddOns.Handler(addOns, tiers).HandleAsync(new SetSeasonAddOns.Command(SeasonId,
        [
            Input("Parkplatz", [7, 70, 8, 999], price: 12.006m, scope: AddOnScope.PerOrder, promoOnly: true, requireMobile: true,
                longTitle: " Parkplatz Saison ", before: "vorher", after: "nachher"),
            Input("   "),
            Input("Garderobe", active: false)
        ]));

        Assert.Equal(SeasonId, addOns.ReplacedSeasonId);
        Assert.NotNull(addOns.Replaced);
        Assert.Equal(2, addOns.Replaced!.Count);

        var parking = addOns.Replaced[0];
        Assert.Equal("Parkplatz", parking.Label);
        Assert.Equal([7, 8], parking.AllowedTierIds.Order());
        Assert.Equal(12.01m, parking.Price);
        Assert.Equal(AddOnScope.PerOrder, parking.Scope);
        Assert.True(parking.PromoOnly);
        Assert.True(parking.RequireMobileNumber);
        Assert.Equal("Parkplatz Saison", parking.LongTitle);
        Assert.Equal("vorher", parking.InfoBeforePurchase);
        Assert.Equal("nachher", parking.InfoAfterPurchase);
        Assert.Equal(SeasonId, parking.SeasonId);

        var wardrobe = addOns.Replaced[1];
        Assert.Equal("Garderobe", wardrobe.Label);
        Assert.False(wardrobe.Active);
        Assert.Empty(wardrobe.AllowedTierIds);
    }

    [Fact]
    public async Task A_negative_price_is_rejected_before_replacing()
    {
        var tiers = new InMemoryPriceTiers();
        var addOns = new RecordingSeasonAddOns();

        await Assert.ThrowsAsync<DomainException>(() =>
            new SetSeasonAddOns.Handler(addOns, tiers).HandleAsync(new SetSeasonAddOns.Command(SeasonId, [Input("Parkplatz", price: -1m)])));

        Assert.Null(addOns.Replaced);
    }

    [Fact]
    public async Task An_empty_list_clears_the_season_add_ons()
    {
        var tiers = new InMemoryPriceTiers();
        var addOns = new RecordingSeasonAddOns();

        await new SetSeasonAddOns.Handler(addOns, tiers).HandleAsync(new SetSeasonAddOns.Command(SeasonId, []));

        Assert.Equal(SeasonId, addOns.ReplacedSeasonId);
        Assert.Empty(addOns.Replaced!);
    }
}
