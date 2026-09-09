using NPoco;
using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Catalog.Infrastructure;
using RedAnts.Ticketing.Features.Checkout;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Features.SeasonPasses.Infrastructure;

public sealed class SeasonPassPricingReader(IScopeProvider scopeProvider) : ISeasonPassPricing
{
    public async Task<IReadOnlyList<AvailableTicketCategory>> GetAvailableAsync(int seasonId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);

        var parent = await scope.Database.FirstOrDefaultAsync<SeasonPriceRecord>("WHERE SeasonId = @0", seasonId);
        if (parent is null) return [];

        var cats = await scope.Database.FetchAsync<SeasonPriceCategoryRecord>("WHERE SeasonPriceId = @0", parent.Id);
        var tiers = await scope.Database.FetchAsync<SeasonPriceTierRecord>("WHERE SeasonId = @0", seasonId);
        var tierById = tiers.ToDictionary(t => t.Id);
        if (tierById.Count == 0) return [];

        var soldByTier = await SoldByTierAsync(scope.Database, seasonId);
        int? totalRemaining = parent.TotalSalesQuota is { } tq
            ? Math.Max(0, tq - await SoldTotalAsync(scope.Database, seasonId) - parent.Reserved) : null;

        var rows = new List<TierRow>();
        foreach (var c in cats)
        {
            if (c.TierId is not { } tid || !tierById.TryGetValue(tid, out var t)) continue;
            rows.Add(new TierRow(tid, t.Name, t.PromoOfTierId, t.SortOrder, c.Offered ?? true,
                c.SalePrice, c.Quota, ToDateOnly(c.PassAvailableFrom), ToDateOnly(c.PassAvailableUntil), soldByTier.GetValueOrDefault(tid) + c.Reserved,
                t.MinAge, t.MaxAge));
        }
        return TierOffer.Resolve(rows, totalRemaining);
    }

    public async Task<AvailableTicketCategory?> FindAvailableByTierAsync(int seasonId, int tierId)
    {
        var all = await GetAvailableAsync(seasonId);
        return all.FirstOrDefault(c => c.TierId == tierId);
    }

    public async Task<string?> CheckCapacityAsync(IReadOnlyList<PassDemand> demand)
    {
        foreach (var group in demand.Where(d => d.Quantity > 0).GroupBy(d => d.SeasonId))
        {
            var available = (await GetAvailableAsync(group.Key)).ToDictionary(a => a.TierId);
            foreach (var tierGroup in group.GroupBy(d => d.TierId))
            {
                if (!available.TryGetValue(tierGroup.Key, out var a) || !a.Available)
                    return "Eine gewählte Preisstufe ist für diese Saison nicht mehr verfügbar.";
                var requested = tierGroup.Sum(d => d.Quantity);
                if (a.Remaining is { } r && requested > r)
                    return $"Für «{a.Name}» sind nur noch {r} Saisonkarten verfügbar.";
            }
        }
        return null;
    }

    public async Task<IReadOnlyDictionary<int, int>> GetSoldCountsAsync(int seasonId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        return await SoldByTierAsync(scope.Database, seasonId);
    }

    private static async Task<Dictionary<int, int>> SoldByTierAsync(IDatabase db, int seasonId)
    {
        var rows = await db.FetchAsync<TierCountRow>(
            "SELECT TierId AS TierId, COUNT(*) AS Cnt FROM SeasonPasses " +
            "WHERE SeasonId = @0 AND Status = @1 AND TierId IS NOT NULL GROUP BY TierId",
            seasonId, (int)TicketStatus.Valid);
        return rows.ToDictionary(r => r.TierId, r => r.Cnt);
    }

    private static async Task<int> SoldTotalAsync(IDatabase db, int seasonId) =>
        await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM SeasonPasses WHERE SeasonId = @0 AND Status = @1", seasonId, (int)TicketStatus.Valid);

    private static DateOnly? ToDateOnly(DateTime? value) => value is { } v ? DateOnly.FromDateTime(v) : null;
}
