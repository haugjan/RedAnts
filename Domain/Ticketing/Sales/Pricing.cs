namespace RedAnts.Domain.Ticketing.Sales;

public sealed class CategoryPrice
{
    public TicketCategory Category { get; private set; }
    public decimal SalePrice { get; private set; }
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

public sealed class EventPrice
{
    public int Id { get; private set; }
    public int EventId { get; private set; }
    public int? TotalSalesQuota { get; private set; }
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

public sealed class SeasonCategoryPrice
{
    public TicketCategory Category { get; private set; }
    public decimal PassPrice { get; private set; }
    public decimal TicketPrice { get; private set; }
    public bool Offered { get; private set; }
    public int? Quota { get; private set; }

    private SeasonCategoryPrice(TicketCategory category, decimal passPrice, decimal ticketPrice, bool offered, int? quota)
    {
        Category = category;
        PassPrice = passPrice;
        TicketPrice = ticketPrice;
        Offered = offered;
        Quota = quota;
    }

    public static SeasonCategoryPrice Create(TicketCategory category, decimal passPrice, decimal ticketPrice, bool offered, int? quota)
    {
        if (passPrice < 0) throw new DomainException("Saisonkarten-Preis darf nicht negativ sein.");
        if (ticketPrice < 0) throw new DomainException("Ticketpreis darf nicht negativ sein.");
        if (quota is < 0) throw new DomainException("Kontingent darf nicht negativ sein.");
        return new SeasonCategoryPrice(category, decimal.Round(passPrice, 2), decimal.Round(ticketPrice, 2), offered, quota);
    }

    public static SeasonCategoryPrice FromPersistence(TicketCategory category, decimal passPrice, decimal ticketPrice, bool offered, int? quota) =>
        new(category, passPrice, ticketPrice, offered, quota);
}

public sealed class SeasonPrice
{
    public int Id { get; private set; }
    public int SeasonId { get; private set; }
    public int? TotalSalesQuota { get; private set; }
    public IReadOnlyList<SeasonCategoryPrice> Categories { get; private set; }

    private SeasonPrice(int id, int seasonId, int? totalSalesQuota, IReadOnlyList<SeasonCategoryPrice> categories)
    {
        Id = id;
        SeasonId = seasonId;
        TotalSalesQuota = totalSalesQuota;
        Categories = categories;
    }

    public static SeasonPrice Create(int seasonId, int? totalSalesQuota, IReadOnlyList<SeasonCategoryPrice> categories)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
        if (totalSalesQuota is < 0) throw new DomainException("Einlasskontingent darf nicht negativ sein.");
        return new SeasonPrice(0, seasonId, totalSalesQuota, categories ?? []);
    }

    public static SeasonPrice FromPersistence(int id, int seasonId, int? totalSalesQuota, IReadOnlyList<SeasonCategoryPrice> categories) =>
        new(id, seasonId, totalSalesQuota, categories ?? []);
}
