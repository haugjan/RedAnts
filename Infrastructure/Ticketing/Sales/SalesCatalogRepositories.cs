using NPoco;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Sales;

/// <summary>Event price set (0..1 per event): a parent row (EventId + event-level quotas) with n
/// category sub-rows. Save replaces the child rows to keep the set in sync.</summary>
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
                Quota = c.Quota
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
            cats.Select(c => CategoryPrice.FromPersistence((TicketCategory)c.Category, c.SalePrice, c.Quota)).ToList());
}

/// <summary>Season price set (0..1 per season): a parent row with n category sub-rows.</summary>
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
        var parent = new SeasonPriceRecord { Id = price.Id, SeasonId = price.SeasonId };
        if (parent.Id == 0) await scope.Database.InsertAsync(parent);
        else await scope.Database.UpdateAsync(parent);

        await scope.Database.ExecuteAsync("DELETE FROM SeasonPriceCategories WHERE SeasonPriceId = @0", parent.Id);
        foreach (var c in price.Categories)
            await scope.Database.InsertAsync(new SeasonPriceCategoryRecord
            {
                SeasonPriceId = parent.Id,
                Category = (int)c.Category,
                SalePrice = c.SalePrice,
                Quota = c.Quota
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
        SeasonPrice.FromPersistence(p.Id, p.SeasonId,
            cats.Select(c => CategoryPrice.FromPersistence((TicketCategory)c.Category, c.SalePrice, c.Quota)).ToList());
}

/// <summary>Resolves purchasable categories for an event from its price set, applying availability:
/// a category is unavailable once its own quota, or the event's total sales quota, is exhausted by the
/// valid EventTickets already issued.</summary>
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

        // Valid tickets already sold, per category and in total, to apply the quotas.
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
            var available = remaining is null || remaining > 0;
            list.Add(new AvailableTicketCategory(category, category.DisplayName(), c.SalePrice, available, remaining));
        }
        return list;
    }

    public async Task<AvailableTicketCategory?> FindAvailableAsync(int eventId, TicketCategory category)
    {
        var all = await GetAvailableAsync(eventId);
        return all.FirstOrDefault(c => c.Category == category);
    }

    /// <summary>Smallest of the two remaining caps; null means "no cap" for that side.</summary>
    private static int? MinRemaining(int? a, int? b) =>
        (a, b) switch
        {
            (null, null) => null,
            (null, var y) => y,
            (var x, null) => x,
            var (x, y) => Math.Min(x!.Value, y!.Value)
        };

    private sealed class CategoryCountRow
    {
        public int Category { get; set; }
        public int Cnt { get; set; }
    }
}
