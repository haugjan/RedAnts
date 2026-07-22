using NPoco;
using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Sales;

public sealed class EventPriceRepository(IScopeProvider scopeProvider) : IEventPrices
{
    public async Task<EventPrice?> GetByEventAsync(int eventId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var parent = await scope.Database.FirstOrDefaultAsync<EventPriceRecord>("WHERE EventId = @0", eventId);
        if (parent is null) return null;
        var cats = await scope.Database.FetchAsync<EventPriceCategoryRecord>(
            "WHERE EventPriceId = @0 ORDER BY Category", parent.Id);
        return Map(parent, cats);
    }

    public async Task<EventPrice> SaveAsync(EventPrice price)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var parent = new EventPriceRecord
        {
            Id = price.Id,
            EventId = price.EventId,
            TotalSalesQuota = price.TotalSalesQuota,
            AdmissionQuota = price.AdmissionQuota
        };
        if (parent.Id == 0) await scope.Database.InsertAsync(parent);
        else await scope.Database.UpdateAsync(parent);

        await scope.Database.ExecuteAsync("DELETE FROM EventPriceCategories WHERE EventPriceId = @0", parent.Id);
        foreach (var c in price.Categories)
            await scope.Database.InsertAsync(new EventPriceCategoryRecord
            {
                EventPriceId = parent.Id,
                Category = (int)c.Category,
                TierId = c.TierId,
                SalePrice = c.SalePrice,
                Quota = c.Quota,
                AvailableUntil = c.AvailableUntil?.ToDateTime(TimeOnly.MinValue),
                ArticleGuid = Guid.NewGuid()
            });

        var cats = await scope.Database.FetchAsync<EventPriceCategoryRecord>(
            "WHERE EventPriceId = @0 ORDER BY Category", parent.Id);
        return Map(parent, cats);
    }

    public async Task DeleteAsync(int eventPriceId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await scope.Database.ExecuteAsync("DELETE FROM EventPriceCategories WHERE EventPriceId = @0", eventPriceId);
        await scope.Database.DeleteAsync(new EventPriceRecord { Id = eventPriceId });
    }

    private static EventPrice Map(EventPriceRecord p, IEnumerable<EventPriceCategoryRecord> cats) =>
        EventPrice.FromPersistence(p.Id, p.EventId, p.TotalSalesQuota, p.AdmissionQuota,
            cats.Select(c => CategoryPrice.FromPersistence(
                (TicketCategory)c.Category, c.SalePrice, c.Quota, ToDateOnly(c.AvailableUntil), c.TierId)).ToList());

    private static DateOnly? ToDateOnly(DateTime? value) => value is { } v ? DateOnly.FromDateTime(v) : null;
}

public sealed class SeasonPriceRepository(IScopeProvider scopeProvider) : ISeasonPrices
{
    public async Task<SeasonPrice?> GetBySeasonAsync(int seasonId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var parent = await scope.Database.FirstOrDefaultAsync<SeasonPriceRecord>("WHERE SeasonId = @0", seasonId);
        if (parent is null) return null;
        var cats = await scope.Database.FetchAsync<SeasonPriceCategoryRecord>(
            "WHERE SeasonPriceId = @0 ORDER BY Category", parent.Id);
        return Map(parent, cats);
    }

    public async Task<SeasonPrice> SaveAsync(SeasonPrice price)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var parent = new SeasonPriceRecord { Id = price.Id, SeasonId = price.SeasonId, TotalSalesQuota = price.TotalSalesQuota, DefaultTicketSalesQuota = price.DefaultTicketSalesQuota };
        if (parent.Id == 0) await scope.Database.InsertAsync(parent);
        else await scope.Database.UpdateAsync(parent);

        await scope.Database.ExecuteAsync("DELETE FROM SeasonPriceCategories WHERE SeasonPriceId = @0", parent.Id);
        foreach (var c in price.Categories)
            await scope.Database.InsertAsync(new SeasonPriceCategoryRecord
            {
                SeasonPriceId = parent.Id,
                Category = (int)c.Category,
                TierId = c.TierId,
                SalePrice = c.PassPrice,
                Quota = c.PassQuota,
                TicketPrice = c.TicketPrice,
                Offered = c.PassOffered,
                TicketOffered = c.TicketOffered,
                TicketQuota = c.TicketQuota,
                PassAvailableUntil = c.PassAvailableUntil?.ToDateTime(TimeOnly.MinValue),
                TicketAvailableUntil = c.TicketAvailableUntil?.ToDateTime(TimeOnly.MinValue),
                ArticleGuid = Guid.NewGuid()
            });

        var cats = await scope.Database.FetchAsync<SeasonPriceCategoryRecord>(
            "WHERE SeasonPriceId = @0 ORDER BY Category", parent.Id);
        return Map(parent, cats);
    }

    public async Task DeleteAsync(int seasonPriceId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await scope.Database.ExecuteAsync("DELETE FROM SeasonPriceCategories WHERE SeasonPriceId = @0", seasonPriceId);
        await scope.Database.DeleteAsync(new SeasonPriceRecord { Id = seasonPriceId });
    }

    private static SeasonPrice Map(SeasonPriceRecord p, IEnumerable<SeasonPriceCategoryRecord> cats) =>
        SeasonPrice.FromPersistence(p.Id, p.SeasonId, p.TotalSalesQuota,
            cats.Select(c => SeasonCategoryPrice.FromPersistence(
                (TicketCategory)c.Category, c.SalePrice, c.Offered ?? true, c.Quota,
                c.TicketPrice ?? 0m, c.TicketOffered ?? c.Offered ?? true, c.TicketQuota,
                ToDateOnly(c.PassAvailableUntil), ToDateOnly(c.TicketAvailableUntil), c.TierId)).ToList(),
            p.DefaultTicketSalesQuota);

    private static DateOnly? ToDateOnly(DateTime? value) => value is { } v ? DateOnly.FromDateTime(v) : null;
}

public sealed class PriceTierRepository(IScopeProvider scopeProvider) : IPriceTiers
{
    public async Task<IReadOnlyList<PriceTier>> GetBySeasonAsync(int seasonId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<SeasonPriceTierRecord>(
            "WHERE SeasonId = @0 ORDER BY SortOrder, Id", seasonId);
        return rows.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<PriceTier>> SaveForSeasonAsync(int seasonId, IReadOnlyList<PriceTierInput> tiers)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;

        var existing = await db.FetchAsync<SeasonPriceTierRecord>("WHERE SeasonId = @0", seasonId);
        var keptIds = new HashSet<int>();
        foreach (var t in tiers)
        {
            if (t.Id > 0) keptIds.Add(t.Id);
            if (t.Promo is { Id: > 0 } p) keptIds.Add(p.Id);
        }

        foreach (var e in existing.Where(e => !keptIds.Contains(e.Id)))
        {
            if (await SoldCountAsync(db, e.Id) > 0)
                throw new DomainException($"Die Stufe «{e.Name}» kann nicht gelöscht werden, weil bereits Karten verkauft wurden.");
            await db.ExecuteAsync("DELETE FROM EventPriceCategories WHERE TierId = @0", e.Id);
            await db.ExecuteAsync("DELETE FROM SeasonPriceCategories WHERE TierId = @0", e.Id);
            await db.DeleteAsync(e);
        }

        foreach (var t in tiers)
        {
            var normal = await UpsertAsync(db, seasonId, t.Id, t.Name, t.MaxAge, null, t.SortOrder);
            if (t.Promo is { } p)
                await UpsertAsync(db, seasonId, p.Id, p.Name, null, normal.Id, t.SortOrder);
        }

        var saved = await db.FetchAsync<SeasonPriceTierRecord>("WHERE SeasonId = @0 ORDER BY SortOrder, Id", seasonId);
        return saved.Select(Map).ToList();
    }

    public async Task<int> GetSoldCountAsync(int tierId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        return await SoldCountAsync(scope.Database, tierId);
    }

    private static async Task<SeasonPriceTierRecord> UpsertAsync(IDatabase db, int seasonId, int id, string name,
        int? maxAge, int? promoOfTierId, int sortOrder)
    {
        var tier = PriceTier.Create(seasonId, name, maxAge, promoOfTierId, sortOrder);
        var rec = new SeasonPriceTierRecord
        {
            Id = id,
            SeasonId = seasonId,
            Name = tier.Name,
            MaxAge = tier.MaxAge,
            PromoOfTierId = tier.PromoOfTierId,
            SortOrder = tier.SortOrder
        };
        if (id == 0) await db.InsertAsync(rec);
        else await db.UpdateAsync(rec);
        return rec;
    }

    private static async Task<int> SoldCountAsync(IDatabase db, int tierId) =>
        await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM EventTickets WHERE TierId = @0", tierId)
        + await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM SeasonSingleTickets WHERE TierId = @0", tierId)
        + await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM SeasonPasses WHERE TierId = @0", tierId);

    private static PriceTier Map(SeasonPriceTierRecord r) =>
        PriceTier.FromPersistence(r.Id, r.SeasonId, r.Name, r.MaxAge, r.PromoOfTierId, r.SortOrder);
}

internal sealed record TierRow(
    int TierId, string Name, int? PromoOfTierId, int SortOrder,
    bool Offered, decimal Price, int? Quota, DateOnly? AvailableUntil, int Sold);

internal static class TierOffer
{
    public static List<AvailableTicketCategory> Resolve(IReadOnlyList<TierRow> rows, int? totalRemaining)
    {
        var promoByParent = rows
            .Where(r => r.PromoOfTierId is not null)
            .ToDictionary(r => r.PromoOfTierId!.Value);

        var result = new List<AvailableTicketCategory>();
        foreach (var normal in rows.Where(r => r.PromoOfTierId is null && r.Offered)
                     .OrderBy(r => r.SortOrder).ThenBy(r => r.TierId))
        {
            if (promoByParent.GetValueOrDefault(normal.TierId) is { Offered: true } promo
                && Pick(promo, totalRemaining) is { Available: true } pick)
            {
                var action = promo.Name.Trim();
                if (action.StartsWith(normal.Name + " ", StringComparison.OrdinalIgnoreCase))
                    action = action[normal.Name.Length..].TrimStart();
                result.Add(pick with
                {
                    ShortName = normal.Name,
                    ActionText = string.IsNullOrWhiteSpace(action) ? null : action,
                    OriginalPrice = pick.Price < normal.Price ? normal.Price : null
                });
                continue;
            }
            result.Add(Pick(normal, totalRemaining));
        }
        return result;
    }

    private static AvailableTicketCategory Pick(TierRow r, int? totalRemaining)
    {
        int? categoryRemaining = r.Quota is { } q ? Math.Max(0, q - r.Sold) : null;
        var remaining = Least(categoryRemaining, totalRemaining);
        var available = (remaining is null || remaining > 0) && NotExpired(r.AvailableUntil);
        return new AvailableTicketCategory(r.TierId, r.Name, r.Price, available, remaining, r.AvailableUntil);
    }

    private static int? Least(int? a, int? b) =>
        (a, b) switch
        {
            (null, null) => null,
            (null, var y) => y,
            (var x, null) => x,
            var (x, y) => Math.Min(x!.Value, y!.Value)
        };

    private static bool NotExpired(DateOnly? until) => until is null || DateOnly.FromDateTime(DateTime.Today) <= until.Value;
}

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
            ? Math.Max(0, tq - await SoldTotalAsync(scope.Database, eventId)) : null;

        var rows = new List<TierRow>();
        foreach (var c in cats)
        {
            if (c.TierId is not { } tid || !tierById.TryGetValue(tid, out var t)) continue;
            rows.Add(new TierRow(tid, t.Name, t.PromoOfTierId, t.SortOrder, true,
                c.SalePrice, c.Quota, ToDateOnly(c.AvailableUntil), soldByTier.GetValueOrDefault(tid)));
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
            var available = (await GetAvailableAsync(evGroup.Key)).ToDictionary(a => a.TierId);
            if (available.Count == 0)
                return "Für einen Anlass im Warenkorb sind keine Tickets mehr verfügbar.";

            using var scope = scopeProvider.CreateScope(autoComplete: true);
            var parent = await scope.Database.FirstOrDefaultAsync<EventPriceRecord>("WHERE EventId = @0", evGroup.Key);
            var requestedTotal = evGroup.Sum(d => d.Quantity);
            if (parent?.TotalSalesQuota is { } tq && await SoldTotalAsync(scope.Database, evGroup.Key) + requestedTotal > tq)
                return "Für einen Anlass im Warenkorb sind nicht mehr genügend Tickets verfügbar.";

            foreach (var tierGroup in evGroup.GroupBy(d => d.TierId))
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
            ? Math.Max(0, tq - await SoldTotalAsync(scope.Database, seasonId)) : null;

        var rows = new List<TierRow>();
        foreach (var c in cats)
        {
            if (c.TierId is not { } tid || !tierById.TryGetValue(tid, out var t)) continue;
            rows.Add(new TierRow(tid, t.Name, t.PromoOfTierId, t.SortOrder, c.Offered ?? true,
                c.SalePrice, c.Quota, ToDateOnly(c.PassAvailableUntil), soldByTier.GetValueOrDefault(tid)));
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

internal sealed class TierCountRow
{
    public int TierId { get; set; }
    public int Cnt { get; set; }
}

public sealed class SeasonAddOnRepository(IScopeProvider scopeProvider) : ISeasonAddOns
{
    public async Task<IReadOnlyList<SeasonAddOn>> GetBySeasonAsync(int seasonId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<SeasonAddOnRecord>(
            "WHERE SeasonId = @0 ORDER BY SortOrder, Id", seasonId);
        return rows.Select(Map).ToList();
    }

    public async Task ReplaceForSeasonAsync(int seasonId, IReadOnlyList<SeasonAddOn> options)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await scope.Database.ExecuteAsync("DELETE FROM SeasonAddOns WHERE SeasonId = @0", seasonId);
        var order = 0;
        foreach (var o in options)
            await scope.Database.InsertAsync(new SeasonAddOnRecord
            {
                SeasonId = seasonId,
                Label = o.Label,
                Price = o.Price,
                Active = o.Active,
                SortOrder = order++,
                Scope = (int)o.Scope,
                InfoBeforePurchase = o.InfoBeforePurchase,
                InfoAfterPurchase = o.InfoAfterPurchase,
                LongTitle = o.LongTitle,
                AllowedTierIds = o.AllowedTierIds.Count == 0 ? null : string.Join(',', o.AllowedTierIds),
                PromoOnly = o.PromoOnly,
                ArticleGuid = Guid.NewGuid()
            });
    }

    private static SeasonAddOn Map(SeasonAddOnRecord r) =>
        SeasonAddOn.FromPersistence(r.Id, r.SeasonId, r.Label, r.Price, r.Active, r.SortOrder, (AddOnScope)r.Scope,
            r.InfoBeforePurchase, r.InfoAfterPurchase, r.LongTitle, ParseTierIds(r.AllowedTierIds), r.PromoOnly);

    private static IReadOnlyList<int> ParseTierIds(string? csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? []
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();
}
