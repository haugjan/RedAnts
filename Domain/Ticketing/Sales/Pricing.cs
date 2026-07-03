namespace RedAnts.Domain.Ticketing.Sales;

/// <summary>The price and sales quota of a single ticket category within an event or season price set.
/// A sub-row of <see cref="EventPrice"/> / <see cref="SeasonPrice"/>.</summary>
public sealed class CategoryPrice
{
    public TicketCategory Category { get; private set; }
    /// <summary>Verkaufspreis (sale price) in CHF.</summary>
    public decimal SalePrice { get; private set; }
    /// <summary>Kontingent: how many tickets of this category may be sold; null = unlimited.</summary>
    public int? Quota { get; private set; }

    private CategoryPrice(TicketCategory category, decimal salePrice, int? quota)
    {
        Category = category;
        SalePrice = salePrice;
        Quota = quota;
    }

    public static CategoryPrice Create(TicketCategory category, decimal salePrice, int? quota)
    {
        if (salePrice < 0) throw new DomainException("Verkaufspreis darf nicht negativ sein.");
        if (quota is < 0) throw new DomainException("Kontingent darf nicht negativ sein.");
        return new CategoryPrice(category, decimal.Round(salePrice, 2), quota);
    }

    public static CategoryPrice FromPersistence(TicketCategory category, decimal salePrice, int? quota) =>
        new(category, salePrice, quota);
}

/// <summary>The ticket prices for one event (linked 1-to-0..1 to an Umbraco event node). Holds the
/// per-category prices plus two event-level caps: <see cref="TotalSalesQuota"/> across all categories
/// and <see cref="AdmissionQuota"/> (max persons admitted). When either the category quota or the total
/// sales quota is reached, that category (resp. all categories) is no longer purchasable.</summary>
public sealed class EventPrice
{
    public int Id { get; private set; }
    public int EventId { get; private set; }
    /// <summary>Verkaufskontingent insgesamt across all categories; null = unlimited.</summary>
    public int? TotalSalesQuota { get; private set; }
    /// <summary>Einlasskontingent: maximum number of persons admitted to the event; null = unlimited.</summary>
    public int? AdmissionQuota { get; private set; }
    public IReadOnlyList<CategoryPrice> Categories { get; private set; }

    private EventPrice(int id, int eventId, int? totalSalesQuota, int? admissionQuota, IReadOnlyList<CategoryPrice> categories)
    {
        Id = id;
        EventId = eventId;
        TotalSalesQuota = totalSalesQuota;
        AdmissionQuota = admissionQuota;
        Categories = categories;
    }

    public static EventPrice Create(int eventId, int? totalSalesQuota, int? admissionQuota, IReadOnlyList<CategoryPrice> categories)
    {
        if (eventId <= 0) throw new DomainException("Ein Anlass muss zugewiesen sein.");
        if (totalSalesQuota is < 0) throw new DomainException("Verkaufskontingent darf nicht negativ sein.");
        if (admissionQuota is < 0) throw new DomainException("Einlasskontingent darf nicht negativ sein.");
        return new EventPrice(0, eventId, totalSalesQuota, admissionQuota, categories ?? []);
    }

    public static EventPrice FromPersistence(int id, int eventId, int? totalSalesQuota, int? admissionQuota,
        IReadOnlyList<CategoryPrice> categories) =>
        new(id, eventId, totalSalesQuota, admissionQuota, categories ?? []);
}

/// <summary>The ticket prices for one season (linked 1-to-0..1 to an Umbraco season node). Holds the
/// per-category prices; season products draw from these.</summary>
public sealed class SeasonPrice
{
    public int Id { get; private set; }
    public int SeasonId { get; private set; }
    public IReadOnlyList<CategoryPrice> Categories { get; private set; }

    private SeasonPrice(int id, int seasonId, IReadOnlyList<CategoryPrice> categories)
    {
        Id = id;
        SeasonId = seasonId;
        Categories = categories;
    }

    public static SeasonPrice Create(int seasonId, IReadOnlyList<CategoryPrice> categories)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
        return new SeasonPrice(0, seasonId, categories ?? []);
    }

    public static SeasonPrice FromPersistence(int id, int seasonId, IReadOnlyList<CategoryPrice> categories) =>
        new(id, seasonId, categories ?? []);
}
