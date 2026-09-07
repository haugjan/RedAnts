using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Admin;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Ticketing.Tests.CatalogWorkflow;

internal sealed class InMemorySeasonPrices : ISeasonPrices
{
    private int _nextId = 1;

    public Dictionary<int, SeasonPrice> Stored { get; } = new();
    public int SaveCalls { get; private set; }

    public void Seed(SeasonPrice price) => Stored[price.SeasonId] = price;

    public Task<SeasonPrice?> GetBySeasonAsync(int seasonId) =>
        Task.FromResult(Stored.GetValueOrDefault(seasonId));

    public Task<SeasonPrice> SaveAsync(SeasonPrice price)
    {
        SaveCalls++;
        var saved = price.Id == 0
            ? SeasonPrice.FromPersistence(_nextId++, price.SeasonId, price.TotalSalesQuota, price.Categories,
                price.DefaultTicketSalesQuota, price.Reserved, price.Version)
            : price;
        Stored[saved.SeasonId] = saved;
        return Task.FromResult(saved);
    }

    public Task DeleteAsync(int seasonPriceId)
    {
        foreach (var (seasonId, price) in Stored.ToList())
            if (price.Id == seasonPriceId) Stored.Remove(seasonId);
        return Task.CompletedTask;
    }

    public Task<CapacityUsage> GetPassUsageAsync(int seasonId) => Task.FromResult(CapacityUsage.None);

    public Task SaveReservationAsync(SeasonPrice price) => Task.CompletedTask;
}

internal sealed class InMemoryPriceTiers : IPriceTiers
{
    private int _nextId = 100;

    public List<PriceTier> Stored { get; } = [];
    public Dictionary<int, int> Sold { get; } = new();
    public int SaveCalls { get; private set; }
    public IReadOnlyList<PriceTierInput>? LastInputs { get; private set; }

    public PriceTier Seed(int id, int seasonId, string name, int? promoOfTierId = null, int sortOrder = 0, int? minAge = null, int? maxAge = null)
    {
        var tier = PriceTier.FromPersistence(id, seasonId, name, minAge, maxAge, promoOfTierId, sortOrder);
        Stored.Add(tier);
        return tier;
    }

    public Task<IReadOnlyList<PriceTier>> GetBySeasonAsync(int seasonId) =>
        Task.FromResult<IReadOnlyList<PriceTier>>(Stored.Where(t => t.SeasonId == seasonId).OrderBy(t => t.SortOrder).ThenBy(t => t.Id).ToList());

    public Task<IReadOnlyList<PriceTier>> SaveForSeasonAsync(int seasonId, IReadOnlyList<PriceTierInput> tiers)
    {
        SaveCalls++;
        LastInputs = tiers;
        var kept = new HashSet<int>();
        foreach (var t in tiers)
        {
            if (t.Id > 0) kept.Add(t.Id);
            if (t.Promo is { Id: > 0 } p) kept.Add(p.Id);
        }
        Stored.RemoveAll(t => t.SeasonId == seasonId && !kept.Contains(t.Id));

        foreach (var t in tiers)
        {
            var normal = Upsert(seasonId, t.Id, t.Name, t.MinAge, t.MaxAge, null, t.SortOrder);
            if (t.Promo is { } promo)
                Upsert(seasonId, promo.Id, promo.Name, null, null, normal.Id, t.SortOrder);
        }
        return GetBySeasonAsync(seasonId);
    }

    public Task<int> GetSoldCountAsync(int tierId) => Task.FromResult(Sold.GetValueOrDefault(tierId));

    private PriceTier Upsert(int seasonId, int id, string name, int? minAge, int? maxAge, int? promoOfTierId, int sortOrder)
    {
        var tier = PriceTier.FromPersistence(id == 0 ? _nextId++ : id, seasonId, name, minAge, maxAge, promoOfTierId, sortOrder);
        Stored.RemoveAll(t => t.Id == tier.Id);
        Stored.Add(tier);
        return tier;
    }
}

internal sealed class RecordingSeasonAddOns : ISeasonAddOns
{
    public List<SeasonAddOn> Existing { get; } = [];
    public int? ReplacedSeasonId { get; private set; }
    public IReadOnlyList<SeasonAddOn>? Replaced { get; private set; }

    public Task<IReadOnlyList<SeasonAddOn>> GetBySeasonAsync(int seasonId) =>
        Task.FromResult<IReadOnlyList<SeasonAddOn>>(Existing.Where(a => a.SeasonId == seasonId).ToList());

    public Task ReplaceForSeasonAsync(int seasonId, IReadOnlyList<SeasonAddOn> options)
    {
        ReplacedSeasonId = seasonId;
        Replaced = options;
        return Task.CompletedTask;
    }
}

internal sealed class RecordingSeasonStatus : ISeasonStatusPublisher
{
    public List<(int SeasonId, SeasonStatus Status)> Calls { get; } = [];

    public Task SetStatusAsync(int seasonId, SeasonStatus status)
    {
        Calls.Add((seasonId, status));
        return Task.CompletedTask;
    }

    public Task SetNameAsync(int seasonId, string name) => throw new NotSupportedException();

    public Task SetPeriodAsync(int seasonId, DateOnly startDate, DateOnly endDate) => throw new NotSupportedException();
}
