using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Catalog.Pricing;
using RedAnts.Ticketing.Tests.Catalog;
using Xunit;

namespace RedAnts.Ticketing.Tests.Catalog.Pricing;

public class PricingQueryTests
{
    private const int SeasonId = 3;
    private const int EventId = 42;

    [Fact]
    public async Task Price_tiers_are_read_for_the_season()
    {
        var reader = new StubSeasonsForAdmin();
        reader.Tiers[SeasonId] = [new PriceTierRow(7, "Erwachsen", null, null, 0, null), new PriceTierRow(70, "Frühbucher", null, null, 0, 7)];

        var tiers = await new GetPriceTiers.Handler(reader).HandleAsync(new GetPriceTiers.Query(SeasonId));

        Assert.Equal(2, tiers.Count);
        Assert.Equal(7, tiers[1].PromoOfTierId);
        Assert.Empty(await new GetPriceTiers.Handler(reader).HandleAsync(new GetPriceTiers.Query(SeasonId + 1)));
    }

    [Fact]
    public async Task Event_tier_prices_are_read_for_the_event()
    {
        var reader = new StubEventsForAdmin();
        reader.TierPrices[EventId] = [new EventTierPrice(7, "Erwachsen", true, 20m, 10, new DateOnly(2026, 12, 31)), new EventTierPrice(8, "Kind", false, 0m, null, null)];

        var prices = await new GetEventTierPrices.Handler(reader).HandleAsync(new GetEventTierPrices.Query(EventId));

        Assert.Equal(2, prices.Count);
        Assert.True(prices[0].Offered);
        Assert.False(prices[1].Offered);
    }

    [Fact]
    public async Task Season_tier_prices_carry_pass_ticket_and_promo_prices()
    {
        var reader = new StubSeasonsForAdmin();
        reader.TierPrices[SeasonId] =
        [
            new SeasonTierPrice(7, "Erwachsen", 18, null, 0, 12,
                new TierPassPrice(true, 300m, 50, null, new DateOnly(2026, 9, 30)),
                new TierTicketPrice(true, 20m, 10, null),
                new SeasonTierPromoPrice(70, "Frühbucher", 4, new TierPassPrice(true, 250m, 5, null, new DateOnly(2026, 7, 31)), TierTicketPrice.None))
        ];

        var prices = await new GetSeasonTierPrices.Handler(reader).HandleAsync(new GetSeasonTierPrices.Query(SeasonId));

        var adult = Assert.Single(prices);
        Assert.Equal(12, adult.Sold);
        Assert.Equal(300m, adult.Pass.Price);
        Assert.Equal("Frühbucher", adult.Promo?.Name);
        Assert.False(adult.Promo?.Ticket.Offered);
    }

    [Fact]
    public async Task Season_add_ons_are_read_for_the_season()
    {
        var reader = new StubSeasonsForAdmin();
        reader.AddOns[SeasonId] = [new SeasonAddOnRow(9, "Livestream", "Livestream-Abo", 30m, true, AddOnScope.PerOrder, null, "Danke", [7], true, false)];

        var addOns = await new GetSeasonAddOns.Handler(reader).HandleAsync(new GetSeasonAddOns.Query(SeasonId));

        var addOn = Assert.Single(addOns);
        Assert.Equal(AddOnScope.PerOrder, addOn.Scope);
        Assert.Equal([7], addOn.AllowedTierIds);
        Assert.True(addOn.PromoOnly);
    }
}
