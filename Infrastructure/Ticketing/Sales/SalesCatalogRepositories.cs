using NPoco;
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
                SalePrice = c.SalePrice,
                Quota = c.Quota,
                AvailableUntil = c.AvailableUntil?.ToDateTime(TimeOnly.MinValue)
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
                (TicketCategory)c.Category, c.SalePrice, c.Quota, ToDateOnly(c.AvailableUntil))).ToList());

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
        var parent = new SeasonPriceRecord { Id = price.Id, SeasonId = price.SeasonId, TotalSalesQuota = price.TotalSalesQuota };
        if (parent.Id == 0) await scope.Database.InsertAsync(parent);
        else await scope.Database.UpdateAsync(parent);

        await scope.Database.ExecuteAsync("DELETE FROM SeasonPriceCategories WHERE SeasonPriceId = @0", parent.Id);
        foreach (var c in price.Categories)
            await scope.Database.InsertAsync(new SeasonPriceCategoryRecord
            {
                SeasonPriceId = parent.Id,
                Category = (int)c.Category,
                SalePrice = c.PassPrice,
                Quota = c.PassQuota,
                TicketPrice = c.TicketPrice,
                Offered = c.PassOffered,
                TicketOffered = c.TicketOffered,
                TicketQuota = c.TicketQuota,
                PassAvailableUntil = c.PassAvailableUntil?.ToDateTime(TimeOnly.MinValue),
                TicketAvailableUntil = c.TicketAvailableUntil?.ToDateTime(TimeOnly.MinValue)
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
                ToDateOnly(c.PassAvailableUntil), ToDateOnly(c.TicketAvailableUntil))).ToList());

    private static DateOnly? ToDateOnly(DateTime? value) => value is { } v ? DateOnly.FromDateTime(v) : null;
}

public sealed class EventPricingReader(IScopeProvider scopeProvider) : IEventPricing
{
    public async Task<IReadOnlyList<AvailableTicketCategory>> GetAvailableAsync(int eventId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);

        var parent = await scope.Database.FirstOrDefaultAsync<EventPriceRecord>("WHERE EventId = @0", eventId);
        if (parent is null) return [];

        var cats = await scope.Database.FetchAsync<EventPriceCategoryRecord>(
            "WHERE EventPriceId = @0 ORDER BY Category", parent.Id);
        if (cats.Count == 0) return [];

        var soldRows = await scope.Database.FetchAsync<CategoryCountRow>(
            "SELECT Category, COUNT(*) AS Cnt FROM EventTickets WHERE EventId = @0 AND Status = @1 GROUP BY Category",
            eventId, (int)TicketStatus.Valid);
        var soldByCategory = soldRows.ToDictionary(r => r.Category, r => r.Cnt);
        var soldTotal = soldRows.Sum(r => r.Cnt);

        int? totalRemaining = parent.TotalSalesQuota is { } tq ? Math.Max(0, tq - soldTotal) : null;

        var list = new List<AvailableTicketCategory>();
        foreach (var c in cats)
        {
            var category = (TicketCategory)c.Category;
            var sold = soldByCategory.GetValueOrDefault(c.Category);
            int? categoryRemaining = c.Quota is { } q ? Math.Max(0, q - sold) : null;

            var remaining = MinRemaining(categoryRemaining, totalRemaining);
            var available = (remaining is null || remaining > 0) && NotExpired(c.AvailableUntil);
            list.Add(new AvailableTicketCategory(category, category.DisplayName(), c.SalePrice, available, remaining));
        }
        return list;
    }

    public async Task<AvailableTicketCategory?> FindAvailableAsync(int eventId, TicketCategory category)
    {
        var all = await GetAvailableAsync(eventId);
        return all.FirstOrDefault(c => c.Category == category);
    }

    public async Task<string?> CheckCapacityAsync(IReadOnlyList<TicketDemand> demand)
    {
        foreach (var evGroup in demand.Where(d => d.Quantity > 0).GroupBy(d => d.EventId))
        {
            using var scope = scopeProvider.CreateScope(autoComplete: true);

            var parent = await scope.Database.FirstOrDefaultAsync<EventPriceRecord>("WHERE EventId = @0", evGroup.Key);
            if (parent is null)
                return "Für einen Anlass im Warenkorb sind keine Tickets mehr verfügbar.";

            var cats = await scope.Database.FetchAsync<EventPriceCategoryRecord>(
                "WHERE EventPriceId = @0", parent.Id);
            var catByCategory = cats.ToDictionary(c => c.Category);

            var soldRows = await scope.Database.FetchAsync<CategoryCountRow>(
                "SELECT Category, COUNT(*) AS Cnt FROM EventTickets WHERE EventId = @0 AND Status = @1 GROUP BY Category",
                evGroup.Key, (int)TicketStatus.Valid);
            var soldByCategory = soldRows.ToDictionary(r => r.Category, r => r.Cnt);

            var requestedTotal = evGroup.Sum(d => d.Quantity);
            if (parent.TotalSalesQuota is { } tq && soldRows.Sum(r => r.Cnt) + requestedTotal > tq)
                return "Für einen Anlass im Warenkorb sind nicht mehr genügend Tickets verfügbar.";

            foreach (var catGroup in evGroup.GroupBy(d => d.Category))
            {
                if (!catByCategory.TryGetValue((int)catGroup.Key, out var c) || !NotExpired(c.AvailableUntil))
                    return $"{catGroup.Key.DisplayName()} ist nicht mehr verfügbar.";
                var requested = catGroup.Sum(d => d.Quantity);
                var sold = soldByCategory.GetValueOrDefault((int)catGroup.Key);
                if (c.Quota is { } q && sold + requested > q)
                    return $"{catGroup.Key.DisplayName()} ist nicht mehr in dieser Anzahl verfügbar.";
            }
        }
        return null;
    }

    private static int? MinRemaining(int? a, int? b) =>
        (a, b) switch
        {
            (null, null) => null,
            (null, var y) => y,
            (var x, null) => x,
            var (x, y) => Math.Min(x!.Value, y!.Value)
        };

    private static bool NotExpired(DateTime? until) => until is null || DateTime.Today <= until.Value.Date;

    private sealed class CategoryCountRow
    {
        public int Category { get; set; }
        public int Cnt { get; set; }
    }
}

public sealed class SeasonPassPricingReader(IScopeProvider scopeProvider) : ISeasonPassPricing
{
    public async Task<IReadOnlyList<AvailableTicketCategory>> GetAvailableAsync(int seasonId)
    {
        var offer = await LoadOfferAsync(seasonId);
        return offer
            .Where(o => o.Offered)
            .Select(o => new AvailableTicketCategory(o.Category, o.Category.DisplayName(), o.PassPrice, o.Available, o.CategoryRemaining))
            .ToList();
    }

    public async Task<string?> CheckCapacityAsync(IReadOnlyList<PassDemand> demand)
    {
        foreach (var group in demand.Where(d => d.Quantity > 0).GroupBy(d => d.SeasonId))
        {
            var offer = (await LoadOfferAsync(group.Key)).ToDictionary(o => o.Category);
            var totalWanted = group.Sum(d => d.Quantity);

            foreach (var d in group)
            {
                if (!offer.TryGetValue(d.Category, out var o) || !o.Offered || !o.Available)
                    return $"«{d.Category.DisplayName()}» ist für diese Saison nicht mehr verfügbar.";
                if (o.CategoryRemaining is { } r && d.Quantity > r)
                    return $"Für «{d.Category.DisplayName()}» sind nur noch {r} Saisonkarten verfügbar.";
            }

            var totalRemaining = offer.Values.FirstOrDefault()?.TotalRemaining;
            if (totalRemaining is { } t && totalWanted > t)
                return $"Für diese Saison sind insgesamt nur noch {t} Saisonkarten verfügbar.";
        }
        return null;
    }

    public async Task<IReadOnlyDictionary<TicketCategory, int>> GetSoldCountsAsync(int seasonId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var soldRows = await scope.Database.FetchAsync<SoldRow>(
            "SELECT Category, COUNT(*) AS Cnt FROM SeasonPasses WHERE SeasonId = @0 AND Status = @1 GROUP BY Category",
            seasonId, (int)TicketStatus.Valid);
        return soldRows.ToDictionary(r => (TicketCategory)r.Category, r => r.Cnt);
    }

    private sealed record OfferRow(
        TicketCategory Category, decimal PassPrice, bool Offered,
        int? CategoryRemaining, int? TotalRemaining, bool Available);

    private async Task<IReadOnlyList<OfferRow>> LoadOfferAsync(int seasonId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);

        var parent = await scope.Database.FirstOrDefaultAsync<SeasonPriceRecord>("WHERE SeasonId = @0", seasonId);
        if (parent is null) return [];

        var cats = await scope.Database.FetchAsync<SeasonPriceCategoryRecord>(
            "WHERE SeasonPriceId = @0 ORDER BY Category", parent.Id);
        if (cats.Count == 0) return [];

        var soldRows = await scope.Database.FetchAsync<SoldRow>(
            "SELECT Category, COUNT(*) AS Cnt FROM SeasonPasses WHERE SeasonId = @0 AND Status = @1 GROUP BY Category",
            seasonId, (int)TicketStatus.Valid);
        var soldByCategory = soldRows.ToDictionary(r => r.Category, r => r.Cnt);
        var soldTotal = soldRows.Sum(r => r.Cnt);

        int? totalRemaining = parent.TotalSalesQuota is { } tq ? Math.Max(0, tq - soldTotal) : null;

        var rows = cats.Select(c =>
        {
            var category = (TicketCategory)c.Category;
            var offered = c.Offered ?? true;
            var sold = soldByCategory.GetValueOrDefault(c.Category);
            int? categoryRemaining = c.Quota is { } q ? Math.Max(0, q - sold) : null;
            var remaining = Least(categoryRemaining, totalRemaining);
            var selfAvailable = offered && (remaining is null || remaining > 0) && NotExpired(c.PassAvailableUntil);
            return new OfferRow(category, c.SalePrice, offered, remaining, totalRemaining, selfAvailable);
        }).ToList();

        var byCategory = rows.ToDictionary(r => r.Category);
        return rows.Select(r =>
        {
            if (r.Category.PromoCounterpart() is { } promo
                && byCategory.TryGetValue(promo, out var promoRow)
                && promoRow.Offered
                && (promoRow.CategoryRemaining is null || promoRow.CategoryRemaining > 0))
            {
                return r with { Available = false };
            }
            return r;
        }).ToList();
    }

    private static int? Least(int? a, int? b) =>
        (a, b) switch
        {
            (null, null) => null,
            (null, var y) => y,
            (var x, null) => x,
            var (x, y) => Math.Min(x!.Value, y!.Value)
        };

    private static bool NotExpired(DateTime? until) => until is null || DateTime.Today <= until.Value.Date;

    private sealed class SoldRow
    {
        public int Category { get; set; }
        public int Cnt { get; set; }
    }
}
