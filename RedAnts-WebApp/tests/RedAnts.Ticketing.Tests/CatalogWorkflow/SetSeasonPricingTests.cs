using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.CatalogWorkflow;
using Xunit;

namespace RedAnts.Ticketing.Tests.CatalogWorkflow;

public class SetSeasonPricingTests
{
    private const int SeasonId = 3;

    private static readonly SeasonPassPricingInput AdultPass = new(true, 300.004m, 50, new DateOnly(2026, 6, 1), new DateOnly(2026, 9, 30));
    private static readonly SeasonTicketPricingInput AdultTicket = new(true, 20m, 10, new DateOnly(2027, 4, 30));
    private static readonly SeasonPassPricingInput PromoPass = new(true, 250m, 5, null, new DateOnly(2026, 7, 31));
    private static readonly SeasonTicketPricingInput PromoTicket = new(false, 0m, null, null);

    private static SeasonTierPricingInput Tier(int id, string name, int promoId = 0, string? promoName = null) =>
        new(id, name, null, null, AdultPass, AdultTicket, promoId, promoName,
            promoName is null ? null : PromoPass, promoName is null ? null : PromoTicket);

    [Fact]
    public async Task New_tiers_get_ids_and_their_categories_point_at_the_saved_tiers()
    {
        var tiers = new InMemoryPriceTiers();
        var prices = new InMemorySeasonPrices();
        var handler = new SetSeasonPricing.Handler(tiers, prices);

        await handler.HandleAsync(new SetSeasonPricing.Command(SeasonId,
        [
            Tier(0, "Erwachsen", promoName: "Frühbucher"),
            Tier(0, "Kind")
        ]));

        var saved = tiers.Stored.Where(t => t.SeasonId == SeasonId).ToList();
        var adult = Assert.Single(saved, t => t.Name == "Erwachsen" && t.PromoOfTierId is null);
        var promo = Assert.Single(saved, t => t.PromoOfTierId == adult.Id);
        var child = Assert.Single(saved, t => t.Name == "Kind");
        Assert.Equal("Frühbucher", promo.Name);
        Assert.Equal(0, adult.SortOrder);
        Assert.Equal(1, child.SortOrder);

        var price = prices.Stored[SeasonId];
        Assert.Equal(3, price.Categories.Count);
        var adultCategory = Assert.Single(price.Categories, c => c.TierId == adult.Id);
        Assert.Equal(300.00m, adultCategory.PassPrice);
        Assert.Equal(50, adultCategory.PassQuota);
        Assert.Equal(new DateOnly(2026, 6, 1), adultCategory.PassAvailableFrom);
        Assert.Equal(20m, adultCategory.TicketPrice);
        Assert.Equal(10, adultCategory.TicketQuota);
        Assert.Equal(new DateOnly(2027, 4, 30), adultCategory.TicketAvailableUntil);
        var promoCategory = Assert.Single(price.Categories, c => c.TierId == promo.Id);
        Assert.Equal(250m, promoCategory.PassPrice);
        Assert.True(promoCategory.PassOffered);
        Assert.False(promoCategory.TicketOffered);
        Assert.Contains(price.Categories, c => c.TierId == child.Id);
    }

    [Fact]
    public async Task Blank_names_are_ignored_and_positions_define_the_sort_order()
    {
        var tiers = new InMemoryPriceTiers();
        var prices = new InMemorySeasonPrices();

        await new SetSeasonPricing.Handler(tiers, prices).HandleAsync(new SetSeasonPricing.Command(SeasonId,
        [
            Tier(0, "  "),
            Tier(0, "Erwachsen"),
            Tier(0, "Kind")
        ]));

        Assert.Equal(["Erwachsen", "Kind"], tiers.LastInputs!.Select(t => t.Name));
        Assert.Equal([0, 1], tiers.LastInputs!.Select(t => t.SortOrder));
        Assert.Equal(2, prices.Stored[SeasonId].Categories.Count);
    }

    [Fact]
    public async Task An_existing_price_row_keeps_its_quotas_and_reservation()
    {
        var tiers = new InMemoryPriceTiers();
        tiers.Seed(7, SeasonId, "Erwachsen");
        var prices = new InMemorySeasonPrices();
        prices.Seed(SeasonPrice.FromPersistence(9, SeasonId, 400,
            [SeasonCategoryPrice.FromPersistence(TicketCategory.Adult, 1m, true, null, 1m, true, null, tierId: 7)],
            defaultTicketSalesQuota: 120, reserved: 4, version: 2));

        await new SetSeasonPricing.Handler(tiers, prices).HandleAsync(new SetSeasonPricing.Command(SeasonId, [Tier(7, "Erwachsen")]));

        var price = prices.Stored[SeasonId];
        Assert.Equal(9, price.Id);
        Assert.Equal(400, price.TotalSalesQuota);
        Assert.Equal(120, price.DefaultTicketSalesQuota);
        Assert.Equal(4, price.Reserved);
        Assert.Equal(300m, Assert.Single(price.Categories).PassPrice);
    }

    [Fact]
    public async Task Removing_a_tier_with_sales_is_rejected_before_anything_is_saved()
    {
        var tiers = new InMemoryPriceTiers();
        tiers.Seed(7, SeasonId, "Erwachsen");
        tiers.Seed(8, SeasonId, "Kind", sortOrder: 1);
        tiers.Sold[8] = 3;
        var prices = new InMemorySeasonPrices();

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            new SetSeasonPricing.Handler(tiers, prices).HandleAsync(new SetSeasonPricing.Command(SeasonId, [Tier(7, "Erwachsen")])));

        Assert.Contains("«Kind»", ex.Message);
        Assert.Equal(0, tiers.SaveCalls);
        Assert.Equal(0, prices.SaveCalls);
        Assert.Contains(tiers.Stored, t => t.Id == 8);
    }

    [Fact]
    public async Task Dropping_a_promo_tier_with_sales_is_rejected()
    {
        var tiers = new InMemoryPriceTiers();
        tiers.Seed(7, SeasonId, "Erwachsen");
        tiers.Seed(70, SeasonId, "Frühbucher", promoOfTierId: 7);
        tiers.Sold[70] = 1;
        var prices = new InMemorySeasonPrices();

        await Assert.ThrowsAsync<DomainException>(() =>
            new SetSeasonPricing.Handler(tiers, prices).HandleAsync(new SetSeasonPricing.Command(SeasonId, [Tier(7, "Erwachsen")])));

        Assert.Equal(0, tiers.SaveCalls);
    }

    [Fact]
    public async Task Removing_an_unsold_tier_deletes_it_and_its_category()
    {
        var tiers = new InMemoryPriceTiers();
        tiers.Seed(7, SeasonId, "Erwachsen");
        tiers.Seed(8, SeasonId, "Kind", sortOrder: 1);
        var prices = new InMemorySeasonPrices();

        await new SetSeasonPricing.Handler(tiers, prices).HandleAsync(new SetSeasonPricing.Command(SeasonId, [Tier(7, "Erwachsen")]));

        Assert.DoesNotContain(tiers.Stored, t => t.Id == 8);
        Assert.Equal(7, Assert.Single(prices.Stored[SeasonId].Categories).TierId);
    }

    [Fact]
    public async Task Tiers_of_other_seasons_are_left_alone()
    {
        var tiers = new InMemoryPriceTiers();
        tiers.Seed(50, 99, "Fremd");
        tiers.Sold[50] = 10;
        var prices = new InMemorySeasonPrices();

        await new SetSeasonPricing.Handler(tiers, prices).HandleAsync(new SetSeasonPricing.Command(SeasonId, [Tier(0, "Erwachsen")]));

        Assert.Contains(tiers.Stored, t => t.Id == 50);
    }
}
