namespace RedAnts.Domain.Ticketing.Sales;

public sealed class SeasonAddOn
{
    public int Id { get; private set; }
    public int SeasonId { get; private set; }
    public string Label { get; private set; }
    public decimal Price { get; private set; }
    public bool Active { get; private set; }
    public int SortOrder { get; private set; }
    public AddOnScope Scope { get; private set; }
    public string? InfoBeforePurchase { get; private set; }
    public string? InfoAfterPurchase { get; private set; }
    public string? LongTitle { get; private set; }
    public IReadOnlyList<int> AllowedTierIds { get; private set; }
    public bool PromoOnly { get; private set; }

    private SeasonAddOn(int id, int seasonId, string label, decimal price, bool active, int sortOrder, AddOnScope scope,
        string? infoBeforePurchase, string? infoAfterPurchase, string? longTitle, IReadOnlyList<int> allowedTierIds, bool promoOnly)
    {
        Id = id;
        SeasonId = seasonId;
        Label = label;
        Price = price;
        Active = active;
        SortOrder = sortOrder;
        Scope = scope;
        InfoBeforePurchase = infoBeforePurchase;
        InfoAfterPurchase = infoAfterPurchase;
        LongTitle = longTitle;
        AllowedTierIds = allowedTierIds;
        PromoOnly = promoOnly;
    }

    public static SeasonAddOn Create(int seasonId, string label, decimal price, bool active, int sortOrder, AddOnScope scope,
        string? infoBeforePurchase = null, string? infoAfterPurchase = null,
        string? longTitle = null, IReadOnlyList<int>? allowedTierIds = null, bool promoOnly = false)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
        var trimmed = (label ?? "").Trim();
        if (trimmed.Length == 0) throw new DomainException("Die Zusatzoption braucht eine Bezeichnung.");
        if (price < 0) throw new DomainException("Der Preis darf nicht negativ sein.");
        return new SeasonAddOn(0, seasonId, trimmed, decimal.Round(price, 2), active, sortOrder, scope,
            string.IsNullOrWhiteSpace(infoBeforePurchase) ? null : infoBeforePurchase.Trim(),
            string.IsNullOrWhiteSpace(infoAfterPurchase) ? null : infoAfterPurchase.Trim(),
            string.IsNullOrWhiteSpace(longTitle) ? null : longTitle.Trim(),
            (allowedTierIds ?? []).Where(t => t > 0).Distinct().ToList(), promoOnly);
    }

    public static SeasonAddOn FromPersistence(int id, int seasonId, string label, decimal price, bool active, int sortOrder, AddOnScope scope,
        string? infoBeforePurchase = null, string? infoAfterPurchase = null,
        string? longTitle = null, IReadOnlyList<int>? allowedTierIds = null, bool promoOnly = false) =>
        new(id, seasonId, label, price, active, sortOrder, scope, infoBeforePurchase, infoAfterPurchase,
            longTitle, allowedTierIds ?? [], promoOnly);
}
