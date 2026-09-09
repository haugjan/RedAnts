using NPoco;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Catalog.Infrastructure;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Features.Checkout.Infrastructure;

public sealed class EventPricingReader(IScopeProvider scopeProvider) : IEventPricing
{
    public async Task<IReadOnlyList<AvailableTicketCategory>> GetAvailableAsync(int eventId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);

        var parent = await scope.Database.FirstOrDefaultAsync<EventPriceRecord>("WHERE EventId = @0", eventId);
        if (parent is null) return [];

        var cats = await scope.Database.FetchAsync<EventPriceCategoryRecord>("WHERE EventPriceId = @0", parent.Id);
        var tierIds = cats.Where(c => c.TierId is not null).Select(c => c.TierId!.Value).Distinct().ToList();
        if (tierIds.Count == 0) return [];

        var tiers = await scope.Database.FetchAsync<SeasonPriceTierRecord>("WHERE Id IN (@0)", tierIds);
        var tierById = tiers.ToDictionary(t => t.Id);

        var soldByTier = await SoldByTierAsync(scope.Database, eventId);
        int? totalRemaining = parent.TotalSalesQuota is { } tq
            ? Math.Max(0, tq - await SoldTotalAsync(scope.Database, eventId) - parent.Reserved) : null;

        var rows = new List<TierRow>();
        foreach (var c in cats)
        {
            if (c.TierId is not { } tid || !tierById.TryGetValue(tid, out var t)) continue;
            rows.Add(new TierRow(tid, t.Name, t.PromoOfTierId, t.SortOrder, true,
                c.SalePrice, c.Quota, null, ToDateOnly(c.AvailableUntil), soldByTier.GetValueOrDefault(tid) + c.Reserved,
                t.MinAge, t.MaxAge));
        }
        return TierOffer.Resolve(rows, totalRemaining);
    }

    public async Task<AvailableTicketCategory?> FindAvailableByTierAsync(int eventId, int tierId)
    {
        var all = await GetAvailableAsync(eventId);
        return all.FirstOrDefault(c => c.TierId == tierId);
    }

    public async Task<string?> CheckCapacityAsync(IReadOnlyList<TicketDemand> demand)
    {
        foreach (var evGroup in demand.Where(d => d.Quantity > 0).GroupBy(d => d.EventId))
        {
            var hasNormalSale = evGroup.Any(d => !d.IsConversion);
            var available = (await GetAvailableAsync(evGroup.Key)).ToDictionary(a => a.TierId);
            if (hasNormalSale && available.Count == 0)
                return "Für einen Anlass im Warenkorb sind keine Tickets mehr verfügbar.";

            using var scope = scopeProvider.CreateScope(autoComplete: true);
            var parent = await scope.Database.FirstOrDefaultAsync<EventPriceRecord>("WHERE EventId = @0", evGroup.Key);
            var requestedTotal = evGroup.Sum(d => d.Quantity);
            if (parent?.TotalSalesQuota is { } tq && await SoldTotalAsync(scope.Database, evGroup.Key) + requestedTotal > tq)
                return "Für einen Anlass im Warenkorb sind nicht mehr genügend Tickets verfügbar.";

            foreach (var tierGroup in evGroup.Where(d => !d.IsConversion).GroupBy(d => d.TierId))
            {
                if (!available.TryGetValue(tierGroup.Key, out var a) || !a.Available)
                    return "Eine gewählte Preisstufe ist nicht mehr verfügbar.";
                var requested = tierGroup.Sum(d => d.Quantity);
                if (a.Remaining is { } r && requested > r)
                    return $"«{a.Name}» ist nicht mehr in dieser Anzahl verfügbar.";
            }
        }
        return null;
    }

    private static async Task<Dictionary<int, int>> SoldByTierAsync(IDatabase db, int eventId)
    {
        var rows = await db.FetchAsync<TierCountRow>(
            "SELECT TierId AS TierId, COUNT(*) AS Cnt FROM EventTickets " +
            "WHERE EventId = @0 AND Status = @1 AND TierId IS NOT NULL GROUP BY TierId",
            eventId, (int)TicketStatus.Valid);
        return rows.ToDictionary(r => r.TierId, r => r.Cnt);
    }

    private static async Task<int> SoldTotalAsync(IDatabase db, int eventId) =>
        await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM EventTickets WHERE EventId = @0 AND Status = @1", eventId, (int)TicketStatus.Valid);

    private static DateOnly? ToDateOnly(DateTime? value) => value is { } v ? DateOnly.FromDateTime(v) : null;
}
