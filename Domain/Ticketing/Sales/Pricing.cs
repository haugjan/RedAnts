namespace RedAnts.Domain.Ticketing.Sales;

public sealed class CategoryPrice
{
    public TicketCategory Category { get; private set; }
    public decimal SalePrice { get; private set; }
    public int? Quota { get; private set; }
    public DateOnly? AvailableUntil { get; private set; }

    private CategoryPrice(TicketCategory category, decimal salePrice, int? quota, DateOnly? availableUntil)
    {
        Category = category;
        SalePrice = salePrice;
        Quota = quota;
        AvailableUntil = availableUntil;
    }

    public static CategoryPrice Create(TicketCategory category, decimal salePrice, int? quota, DateOnly? availableUntil = null)
    {
        if (salePrice < 0) throw new DomainException("Verkaufspreis darf nicht negativ sein.");
        if (quota is < 0) throw new DomainException("Kontingent darf nicht negativ sein.");
        return new CategoryPrice(category, decimal.Round(salePrice, 2), quota, availableUntil);
    }

    public static CategoryPrice FromPersistence(TicketCategory category, decimal salePrice, int? quota, DateOnly? availableUntil = null) =>
        new(category, salePrice, quota, availableUntil);
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
    public bool PassOffered { get; private set; }
    public int? PassQuota { get; private set; }
    public DateOnly? PassAvailableUntil { get; private set; }
    public decimal TicketPrice { get; private set; }
    public bool TicketOffered { get; private set; }
    public int? TicketQuota { get; private set; }
    public DateOnly? TicketAvailableUntil { get; private set; }

    private SeasonCategoryPrice(TicketCategory category, decimal passPrice, bool passOffered, int? passQuota,
        DateOnly? passAvailableUntil, decimal ticketPrice, bool ticketOffered, int? ticketQuota, DateOnly? ticketAvailableUntil)
    {
        Category = category;
        PassPrice = passPrice;
        PassOffered = passOffered;
        PassQuota = passQuota;
        PassAvailableUntil = passAvailableUntil;
        TicketPrice = ticketPrice;
        TicketOffered = ticketOffered;
        TicketQuota = ticketQuota;
        TicketAvailableUntil = ticketAvailableUntil;
    }

    public static SeasonCategoryPrice Create(TicketCategory category, decimal passPrice, bool passOffered, int? passQuota,
        decimal ticketPrice, bool ticketOffered, int? ticketQuota,
        DateOnly? passAvailableUntil = null, DateOnly? ticketAvailableUntil = null)
    {
        if (passPrice < 0) throw new DomainException("Saisonkarten-Preis darf nicht negativ sein.");
        if (ticketPrice < 0) throw new DomainException("Ticketpreis darf nicht negativ sein.");
        if (passQuota is < 0 || ticketQuota is < 0) throw new DomainException("Kontingent darf nicht negativ sein.");
        return new SeasonCategoryPrice(category, decimal.Round(passPrice, 2), passOffered, passQuota, passAvailableUntil,
            decimal.Round(ticketPrice, 2), ticketOffered, ticketQuota, ticketAvailableUntil);
    }

    public static SeasonCategoryPrice FromPersistence(TicketCategory category, decimal passPrice, bool passOffered,
        int? passQuota, decimal ticketPrice, bool ticketOffered, int? ticketQuota,
        DateOnly? passAvailableUntil = null, DateOnly? ticketAvailableUntil = null) =>
        new(category, passPrice, passOffered, passQuota, passAvailableUntil,
            ticketPrice, ticketOffered, ticketQuota, ticketAvailableUntil);
}

public sealed class SeasonPrice
{
    public int Id { get; private set; }
    public int SeasonId { get; private set; }
    public int? TotalSalesQuota { get; private set; }
    public int? DefaultTicketSalesQuota { get; private set; }
    public IReadOnlyList<SeasonCategoryPrice> Categories { get; private set; }

    private SeasonPrice(int id, int seasonId, int? totalSalesQuota, int? defaultTicketSalesQuota, IReadOnlyList<SeasonCategoryPrice> categories)
    {
        Id = id;
        SeasonId = seasonId;
        TotalSalesQuota = totalSalesQuota;
        DefaultTicketSalesQuota = defaultTicketSalesQuota;
        Categories = categories;
    }

    public static SeasonPrice Create(int seasonId, int? totalSalesQuota, IReadOnlyList<SeasonCategoryPrice> categories, int? defaultTicketSalesQuota = null)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
        if (totalSalesQuota is < 0) throw new DomainException("Einlasskontingent darf nicht negativ sein.");
        if (defaultTicketSalesQuota is < 0) throw new DomainException("Verkaufskontingent darf nicht negativ sein.");
        return new SeasonPrice(0, seasonId, totalSalesQuota, defaultTicketSalesQuota, categories ?? []);
    }

    public static SeasonPrice FromPersistence(int id, int seasonId, int? totalSalesQuota, IReadOnlyList<SeasonCategoryPrice> categories, int? defaultTicketSalesQuota = null) =>
        new(id, seasonId, totalSalesQuota, defaultTicketSalesQuota, categories ?? []);
}
