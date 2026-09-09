using NPoco;
using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Admin;
using RedAnts.Ticketing.Features.Catalog.Pricing;
using RedAnts.Ticketing.Features.SeasonPasses;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Features.Catalog.Infrastructure;

public sealed class SeasonsForAdminReader(
    ISeasonReader seasons,
    IEventReader events,
    ISeasonStatsReader stats,
    ISeasonLinkReader links,
    IContentCreateLinks createLinks,
    ISeasonPassPricing passPricing,
    IScopeProvider scopeProvider) : ISeasonsForAdminReader
{
    public async Task<SeasonsForAdmin> GetAllAsync()
    {
        var seasonLinks = await links.GetAllAsync();
        var rows = new List<SeasonForAdmin>();
        foreach (var s in await OrderedSeasonsAsync())
        {
            var eventIds = (await events.GetBySeasonAsync(s.Id)).Select(e => e.Id).ToList();
            var seasonStats = await stats.GetAsync(s.Id, eventIds);
            var quotas = await QuotasAsync(s.Id);
            var link = seasonLinks.GetValueOrDefault(s.Id);
            rows.Add(new SeasonForAdmin(s.Id, s.Name, s.StartDate, s.EndDate, s.Status, eventIds.Count,
                quotas.PassQuota, quotas.DefaultTicketSalesQuota,
                seasonStats.PassesSold, seasonStats.TicketsSold, seasonStats.FlexTickets, seasonStats.Admissions,
                quotas.AddOnCount, link?.Public, link?.Intern));
        }
        return new SeasonsForAdmin(rows, await createLinks.CreateSeasonUrlAsync());
    }

    public async Task<IReadOnlyList<SeasonChoice>> GetChoicesAsync()
    {
        var ordered = await OrderedSeasonsAsync();
        var current = CurrentSeason(ordered);
        return ordered.Select(s => new SeasonChoice(s.Id, s.Name, s.StartDate, s.EndDate, s.Id == current?.Id)).ToList();
    }

    public async Task<IReadOnlyList<PriceTierRow>> GetTiersAsync(int seasonId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<SeasonPriceTierRecord>("WHERE SeasonId = @0 ORDER BY SortOrder, Id", seasonId);
        return rows.Select(t => new PriceTierRow(t.Id, t.Name, t.MinAge, t.MaxAge, t.SortOrder, t.PromoOfTierId, t.LegacyCategory)).ToList();
    }

    public async Task<IReadOnlyList<SeasonTierPrice>> GetTierPricesAsync(int seasonId)
    {
        var sold = await passPricing.GetSoldCountsAsync(seasonId);
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        var tiers = await db.FetchAsync<SeasonPriceTierRecord>("WHERE SeasonId = @0 ORDER BY SortOrder, Id", seasonId);
        var parent = await db.FirstOrDefaultAsync<SeasonPriceRecord>("WHERE SeasonId = @0", seasonId);
        var categories = parent is null
            ? []
            : await db.FetchAsync<SeasonPriceCategoryRecord>("WHERE SeasonPriceId = @0", parent.Id);
        var byTier = new Dictionary<int, SeasonPriceCategoryRecord>();
        foreach (var c in categories.Where(c => c.TierId is not null))
            byTier[c.TierId!.Value] = c;
        var promoByParent = tiers.Where(t => t.PromoOfTierId is not null).ToDictionary(t => t.PromoOfTierId!.Value);

        return tiers.Where(t => t.PromoOfTierId is null)
            .Select(t => new SeasonTierPrice(t.Id, t.Name, t.MinAge, t.MaxAge, t.SortOrder, sold.GetValueOrDefault(t.Id),
                PassPrice(byTier.GetValueOrDefault(t.Id)), TicketPrice(byTier.GetValueOrDefault(t.Id)),
                promoByParent.GetValueOrDefault(t.Id) is { } promo
                    ? new SeasonTierPromoPrice(promo.Id, promo.Name, sold.GetValueOrDefault(promo.Id),
                        PassPrice(byTier.GetValueOrDefault(promo.Id)), TicketPrice(byTier.GetValueOrDefault(promo.Id)))
                    : null))
            .ToList();
    }

    public async Task<IReadOnlyList<SeasonAddOnRow>> GetAddOnsAsync(int seasonId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<SeasonAddOnRecord>("WHERE SeasonId = @0 ORDER BY SortOrder, Id", seasonId);
        return rows.Select(r => new SeasonAddOnRow(r.Id, r.Label, r.LongTitle, r.Price, r.Active, (AddOnScope)r.Scope,
                r.InfoBeforePurchase, r.InfoAfterPurchase, SeasonAddOnRepository.ParseTierIds(r.AllowedTierIds), r.PromoOnly, r.RequireMobileNumber))
            .ToList();
    }

    private async Task<IReadOnlyList<Season>> OrderedSeasonsAsync() =>
        (await seasons.GetAllAsync()).OrderByDescending(s => s.StartDate).ToList();

    private async Task<SeasonQuotas> QuotasAsync(int seasonId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        var price = await db.FirstOrDefaultAsync<SeasonPriceRecord>("WHERE SeasonId = @0", seasonId);
        var addOnCount = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM SeasonAddOns WHERE SeasonId = @0", seasonId);
        return new SeasonQuotas(price?.TotalSalesQuota, price?.DefaultTicketSalesQuota, addOnCount);
    }

    private static Season? CurrentSeason(IReadOnlyList<Season> ordered)
    {
        var today = SwissTime.Today;
        return ordered.FirstOrDefault(s => s.StartDate <= today && today <= s.EndDate)
            ?? ordered.Where(s => s.StartDate <= today).MaxBy(s => s.StartDate);
    }

    private static TierPassPrice PassPrice(SeasonPriceCategoryRecord? c) =>
        c is null
            ? TierPassPrice.None
            : new TierPassPrice(c.Offered ?? true, decimal.Round(c.SalePrice, 2), c.Quota, ToDateOnly(c.PassAvailableFrom), ToDateOnly(c.PassAvailableUntil));

    private static TierTicketPrice TicketPrice(SeasonPriceCategoryRecord? c) =>
        c is null
            ? TierTicketPrice.None
            : new TierTicketPrice(c.TicketOffered ?? c.Offered ?? true, decimal.Round(c.TicketPrice ?? 0m, 2), c.TicketQuota, ToDateOnly(c.TicketAvailableUntil));

    private static DateOnly? ToDateOnly(DateTime? value) => value is { } v ? DateOnly.FromDateTime(v) : null;

    private sealed record SeasonQuotas(int? PassQuota, int? DefaultTicketSalesQuota, int AddOnCount);
}
