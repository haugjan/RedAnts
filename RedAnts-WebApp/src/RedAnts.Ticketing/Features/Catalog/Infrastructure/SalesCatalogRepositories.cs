using NPoco;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Checkout;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Features.Catalog.Infrastructure;

public sealed class EventPriceRepository(IScopeProvider scopeProvider) : IEventPriceRepository
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
            AdmissionQuota = price.AdmissionQuota,
            ConversionOnly = price.ConversionOnly
        };
        if (parent.Id == 0) await scope.Database.InsertAsync(parent);
        else await scope.Database.UpdateAsync(parent, new[] { "EventId", "TotalSalesQuota", "AdmissionQuota", "ConversionOnly" });

        var existing = await scope.Database.FetchAsync<EventPriceCategoryRecord>("WHERE EventPriceId = @0", parent.Id);
        var articles = ArticleGuids.ByTierAndCategory(existing, r => r.TierId, r => r.Category, r => r.ArticleGuid);
        var reserved = ReservedCounts.ByTierAndCategory(existing, r => r.TierId, r => r.Category, r => r.Reserved);
        await scope.Database.ExecuteAsync("DELETE FROM EventPriceCategories WHERE EventPriceId = @0", parent.Id);
        foreach (var c in price.Categories)
            await scope.Database.InsertAsync(new EventPriceCategoryRecord
            {
                EventPriceId = parent.Id,
                Category = (int)c.Category,
                TierId = c.TierId,
                SalePrice = c.SalePrice.Amount,
                Quota = c.Quota,
                AvailableUntil = c.AvailableUntil?.ToDateTime(TimeOnly.MinValue),
                ArticleGuid = articles.Keep(c.TierId, (int)c.Category),
                Reserved = reserved.Keep(c.TierId, (int)c.Category)
            });

        var stored = await scope.Database.SingleByIdAsync<EventPriceRecord>(parent.Id);
        var cats = await scope.Database.FetchAsync<EventPriceCategoryRecord>(
            "WHERE EventPriceId = @0 ORDER BY Category", parent.Id);
        return Map(stored, cats);
    }

    public async Task DeleteAsync(int eventPriceId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await scope.Database.ExecuteAsync("DELETE FROM EventPriceCategories WHERE EventPriceId = @0", eventPriceId);
        await scope.Database.DeleteAsync(new EventPriceRecord { Id = eventPriceId });
    }

    public async Task SaveReservationAsync(EventPrice price)
    {
        using var scope = scopeProvider.CreateScope();
        var affected = await scope.Database.ExecuteAsync(
            "UPDATE EventPrices SET Reserved = @0, Version = Version + 1 WHERE Id = @1 AND Version = @2",
            price.Reserved, price.Id, price.Version);
        if (affected == 0) throw new ConcurrencyException("Das Kontingent wurde gleichzeitig verändert.");
        foreach (var c in price.Categories.Where(c => c.TierId is not null))
            await scope.Database.ExecuteAsync(
                "UPDATE EventPriceCategories SET Reserved = @0 WHERE EventPriceId = @1 AND TierId = @2 AND Category = @3",
                c.Reserved, price.Id, c.TierId, (int)c.Category);
        scope.Complete();
    }

    private static EventPrice Map(EventPriceRecord p, IEnumerable<EventPriceCategoryRecord> cats) =>
        EventPrice.FromPersistence(p.Id, p.EventId, p.TotalSalesQuota, p.AdmissionQuota,
            cats.Select(c => CategoryPrice.FromPersistence(
                (TicketCategory)c.Category, c.SalePrice, c.Quota, ToDateOnly(c.AvailableUntil), c.TierId, c.Reserved)).ToList(),
            p.ConversionOnly, p.Reserved, p.Version);

    private static DateOnly? ToDateOnly(DateTime? value) => value is { } v ? DateOnly.FromDateTime(v) : null;
}

internal sealed class ReservedCounts(IReadOnlyDictionary<string, int> existing)
{
    public static ReservedCounts ByTierAndCategory<T>(IEnumerable<T> rows, Func<T, int?> tierId, Func<T, int> category, Func<T, int> reserved)
    {
        var map = new Dictionary<string, int>();
        foreach (var row in rows)
            map[Key(tierId(row), category(row))] = reserved(row);
        return new ReservedCounts(map);
    }

    public int Keep(int? tierId, int category) => existing.TryGetValue(Key(tierId, category), out var value) ? value : 0;

    private static string Key(int? tierId, int category) => $"{tierId ?? 0}:{category}";
}

public sealed class SeasonPriceRepository(IScopeProvider scopeProvider) : ISeasonPriceRepository
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
        else await scope.Database.UpdateAsync(parent, new[] { "SeasonId", "TotalSalesQuota", "DefaultTicketSalesQuota" });

        var existing = await scope.Database.FetchAsync<SeasonPriceCategoryRecord>("WHERE SeasonPriceId = @0", parent.Id);
        var articles = ArticleGuids.ByTierAndCategory(existing, r => r.TierId, r => r.Category, r => r.ArticleGuid);
        var reserved = ReservedCounts.ByTierAndCategory(existing, r => r.TierId, r => r.Category, r => r.Reserved);
        await scope.Database.ExecuteAsync("DELETE FROM SeasonPriceCategories WHERE SeasonPriceId = @0", parent.Id);
        foreach (var c in price.Categories)
            await scope.Database.InsertAsync(new SeasonPriceCategoryRecord
            {
                SeasonPriceId = parent.Id,
                Category = (int)c.Category,
                TierId = c.TierId,
                SalePrice = c.PassPrice.Amount,
                Quota = c.PassQuota,
                TicketPrice = c.TicketPrice.Amount,
                Offered = c.PassOffered,
                TicketOffered = c.TicketOffered,
                TicketQuota = c.TicketQuota,
                PassAvailableFrom = c.PassAvailableFrom?.ToDateTime(TimeOnly.MinValue),
                PassAvailableUntil = c.PassAvailableUntil?.ToDateTime(TimeOnly.MinValue),
                TicketAvailableUntil = c.TicketAvailableUntil?.ToDateTime(TimeOnly.MinValue),
                ArticleGuid = articles.Keep(c.TierId, (int)c.Category),
                Reserved = reserved.Keep(c.TierId, (int)c.Category)
            });

        var stored = await scope.Database.SingleByIdAsync<SeasonPriceRecord>(parent.Id);
        var cats = await scope.Database.FetchAsync<SeasonPriceCategoryRecord>(
            "WHERE SeasonPriceId = @0 ORDER BY Category", parent.Id);
        return Map(stored, cats);
    }

    public async Task DeleteAsync(int seasonPriceId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await scope.Database.ExecuteAsync("DELETE FROM SeasonPriceCategories WHERE SeasonPriceId = @0", seasonPriceId);
        await scope.Database.DeleteAsync(new SeasonPriceRecord { Id = seasonPriceId });
    }

    public async Task SaveReservationAsync(SeasonPrice price)
    {
        using var scope = scopeProvider.CreateScope();
        var affected = await scope.Database.ExecuteAsync(
            "UPDATE SeasonPrices SET Reserved = @0, Version = Version + 1 WHERE Id = @1 AND Version = @2",
            price.Reserved, price.Id, price.Version);
        if (affected == 0) throw new ConcurrencyException("Das Kontingent wurde gleichzeitig verändert.");
        foreach (var c in price.Categories.Where(c => c.TierId is not null))
            await scope.Database.ExecuteAsync(
                "UPDATE SeasonPriceCategories SET Reserved = @0 WHERE SeasonPriceId = @1 AND TierId = @2 AND Category = @3",
                c.Reserved, price.Id, c.TierId, (int)c.Category);
        scope.Complete();
    }

    private static SeasonPrice Map(SeasonPriceRecord p, IEnumerable<SeasonPriceCategoryRecord> cats) =>
        SeasonPrice.FromPersistence(p.Id, p.SeasonId, p.TotalSalesQuota,
            cats.Select(c => SeasonCategoryPrice.FromPersistence(
                (TicketCategory)c.Category, c.SalePrice, c.Offered ?? true, c.Quota,
                c.TicketPrice ?? 0m, c.TicketOffered ?? c.Offered ?? true, c.TicketQuota,
                ToDateOnly(c.PassAvailableFrom), ToDateOnly(c.PassAvailableUntil), ToDateOnly(c.TicketAvailableUntil), c.TierId,
                c.Reserved)).ToList(),
            p.DefaultTicketSalesQuota, p.Reserved, p.Version);

    private static DateOnly? ToDateOnly(DateTime? value) => value is { } v ? DateOnly.FromDateTime(v) : null;
}

public sealed class PriceTierRepository(IScopeProvider scopeProvider) : IPriceTierRepository
{
    public async Task<SeasonPriceTiers> LoadSeasonAsync(int seasonId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<SeasonPriceTierRecord>(
            "WHERE SeasonId = @0 ORDER BY SortOrder, Id", seasonId);
        return new SeasonPriceTiers(seasonId, rows.Select(Map).ToList());
    }

    public async Task<SeasonPriceTiers> SaveForSeasonAsync(int seasonId, IReadOnlyList<PriceTierInput> tiers)
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
            await db.ExecuteAsync("DELETE FROM EventPriceCategories WHERE TierId = @0", e.Id);
            await db.ExecuteAsync("DELETE FROM SeasonPriceCategories WHERE TierId = @0", e.Id);
            await db.DeleteAsync(e);
        }

        foreach (var t in tiers)
        {
            var normal = await UpsertAsync(db, seasonId, t.Id, t.Name, t.MinAge, t.MaxAge, null, t.SortOrder);
            if (t.Promo is { } p)
                await UpsertAsync(db, seasonId, p.Id, p.Name, null, null, normal.Id, t.SortOrder);
        }

        var saved = await db.FetchAsync<SeasonPriceTierRecord>("WHERE SeasonId = @0 ORDER BY SortOrder, Id", seasonId);
        return new SeasonPriceTiers(seasonId, saved.Select(Map).ToList());
    }

    private static async Task<SeasonPriceTierRecord> UpsertAsync(IDatabase db, int seasonId, int id, string name,
        int? minAge, int? maxAge, int? promoOfTierId, int sortOrder)
    {
        var tier = PriceTier.Create(seasonId, name, minAge, maxAge, promoOfTierId, sortOrder);
        var rec = new SeasonPriceTierRecord
        {
            Id = id,
            SeasonId = seasonId,
            Name = tier.Name,
            MinAge = tier.MinAge,
            MaxAge = tier.MaxAge,
            PromoOfTierId = tier.PromoOfTierId,
            SortOrder = tier.SortOrder
        };
        if (id == 0) await db.InsertAsync(rec);
        else await db.UpdateAsync(rec);
        return rec;
    }

    private static PriceTier Map(SeasonPriceTierRecord r) =>
        PriceTier.FromPersistence(r.Id, r.SeasonId, r.Name, r.MinAge, r.MaxAge, r.PromoOfTierId, r.SortOrder, r.LegacyCategory);
}

internal sealed record TierRow(
    int TierId, string Name, int? PromoOfTierId, int SortOrder,
    bool Offered, decimal Price, int? Quota, DateOnly? AvailableFrom, DateOnly? AvailableUntil, int Sold,
    int? MinAge = null, int? MaxAge = null);

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
                    OriginalPrice = pick.Price < normal.Price ? normal.Price : null,
                    MinAge = normal.MinAge,
                    MaxAge = normal.MaxAge
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
        var available = (remaining is null || remaining > 0) && InSaleWindow(r.AvailableFrom, r.AvailableUntil);
        return new AvailableTicketCategory(r.TierId, r.Name, r.Price, available, remaining, r.AvailableUntil)
        {
            MinAge = r.MinAge,
            MaxAge = r.MaxAge
        };
    }

    private static int? Least(int? a, int? b) =>
        (a, b) switch
        {
            (null, null) => null,
            (null, var y) => y,
            (var x, null) => x,
            var (x, y) => Math.Min(x!.Value, y!.Value)
        };

    private static bool InSaleWindow(DateOnly? from, DateOnly? until)
    {
        var today = SwissTime.Today;
        return (from is null || today >= from.Value) && (until is null || today <= until.Value);
    }
}

internal sealed class TierCountRow
{
    public int TierId { get; set; }
    public int Cnt { get; set; }
}

public sealed class SeasonAddOnRepository(IScopeProvider scopeProvider) : ISeasonAddOnRepository
{
    public async Task<SeasonAddOnSet> LoadSeasonAsync(int seasonId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<SeasonAddOnRecord>(
            "WHERE SeasonId = @0 ORDER BY SortOrder, Id", seasonId);
        return new SeasonAddOnSet(seasonId, rows.Select(Map).ToList());
    }

    public async Task ReplaceForSeasonAsync(int seasonId, IReadOnlyList<SeasonAddOn> options)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var articles = ArticleGuids.ByIdOrLabel(await scope.Database.FetchAsync<SeasonAddOnRecord>(
            "WHERE SeasonId = @0", seasonId), r => r.Id, r => r.Label, r => r.ArticleGuid);
        await scope.Database.ExecuteAsync("DELETE FROM SeasonAddOns WHERE SeasonId = @0", seasonId);
        var order = 0;
        foreach (var o in options)
            await scope.Database.InsertAsync(new SeasonAddOnRecord
            {
                SeasonId = seasonId,
                Label = o.Label,
                Price = o.Price.Amount,
                Active = o.Active,
                SortOrder = order++,
                Scope = (int)o.Scope,
                InfoBeforePurchase = o.InfoBeforePurchase,
                InfoAfterPurchase = o.InfoAfterPurchase,
                LongTitle = o.LongTitle,
                AllowedTierIds = o.AllowedTierIds.Count == 0 ? null : string.Join(',', o.AllowedTierIds),
                PromoOnly = o.PromoOnly,
                RequireMobileNumber = o.RequireMobileNumber,
                ArticleGuid = articles.Keep(o.Id, o.Label)
            });
    }

    private static SeasonAddOn Map(SeasonAddOnRecord r) =>
        SeasonAddOn.FromPersistence(r.Id, r.SeasonId, r.Label, r.Price, r.Active, r.SortOrder, (AddOnScope)r.Scope,
            r.InfoBeforePurchase, r.InfoAfterPurchase, r.LongTitle, ParseTierIds(r.AllowedTierIds), r.PromoOnly, r.RequireMobileNumber);

    internal static IReadOnlyList<int> ParseTierIds(string? csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? []
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();
}
