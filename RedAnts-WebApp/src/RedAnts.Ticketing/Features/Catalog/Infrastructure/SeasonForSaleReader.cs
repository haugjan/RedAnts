using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Catalog.Shop;
using RedAnts.Ticketing.Features.Checkout;
using RedAnts.Ticketing.Features.SeasonPasses;

namespace RedAnts.Ticketing.Features.Catalog.Infrastructure;

public sealed class SeasonForSaleReader(
    ISeasonReader seasons,
    IEventReader events,
    IVenueReader venues,
    IEventPricing pricing,
    ISeasonPassPricing passPricing,
    ISeasonAddOnRepository addOns,
    IPriceTierRepository tiers,
    IContentUrls urls) : ISeasonForSaleReader
{
    public async Task<SeasonForSale?> FindAsync(int seasonId)
    {
        var season = await seasons.FindByIdAsync(seasonId);
        if (season is null) return null;

        var today = SwissTime.Today;
        var venueNames = new VenueNames(venues);
        var upcoming = new List<UpcomingEvent>();
        foreach (var e in (await events.GetBySeasonAsync(seasonId)).Where(e => e.Status == EventStatus.Open && e.Date >= today))
            upcoming.Add(SaleOffers.ToUpcoming(e, await venueNames.ForAsync(e.VenueId), urls.GetUrl(e.Id), await pricing.GetAvailableAsync(e.Id)));
        return new SeasonForSale(season.Id, season.Name, season.StartDate, season.EndDate, season.ImageUrl, urls.GetUrl(season.Id), upcoming);
    }

    public async Task<IReadOnlyList<SeasonPassOffers>> GetPassOffersAsync()
    {
        var result = new List<SeasonPassOffers>();
        foreach (var season in await seasons.GetPublicOpenAsync())
            result.Add(new SeasonPassOffers(season.Id, season.Name, urls.GetUrl(season.Id), await OffersAsync(season.Id)));
        return result;
    }

    private async Task<IReadOnlyList<PassOffer>> OffersAsync(int seasonId)
    {
        var categories = (await passPricing.GetAvailableAsync(seasonId)).Where(c => c.Available).ToList();
        if (categories.Count == 0) return [];

        var activeAddOns = (await addOns.LoadSeasonAsync(seasonId)).Active.ToList();
        var baseTierIds = (await tiers.LoadSeasonAsync(seasonId)).Tiers.ToDictionary(t => t.Id, t => t.PromoOfTierId ?? t.Id);
        return categories.Select(c => new PassOffer(c, AddOnsFor(c, activeAddOns, baseTierIds))).ToList();
    }

    private static IReadOnlyList<PassAddOn> AddOnsFor(AvailableTicketCategory category, List<SeasonAddOn> addOns, Dictionary<int, int> baseTierIds)
    {
        var baseTierId = baseTierIds.GetValueOrDefault(category.TierId, category.TierId);
        var isPromoOffer = baseTierId != category.TierId;
        return addOns
            .Where(a => (a.AllowedTierIds.Count == 0 || a.AllowedTierIds.Contains(baseTierId)) && (!a.PromoOnly || isPromoOffer))
            .Select(a => new PassAddOn(a.Id, a.Label, a.LongTitle, a.Price, a.Scope == AddOnScope.PerOrder, a.InfoBeforePurchase))
            .ToList();
    }
}
