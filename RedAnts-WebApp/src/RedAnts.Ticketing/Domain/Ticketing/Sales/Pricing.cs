namespace RedAnts.Domain.Ticketing.Sales;

public sealed class CategoryPrice
{
    public TicketCategory Category { get; private set; }
    public int? TierId { get; private set; }
    public decimal SalePrice { get; private set; }
    public int? Quota { get; private set; }
    public DateOnly? AvailableUntil { get; private set; }
    public int Reserved { get; private set; }

    private CategoryPrice(TicketCategory category, int? tierId, decimal salePrice, int? quota, DateOnly? availableUntil, int reserved)
    {
        Category = category;
        TierId = tierId;
        SalePrice = salePrice;
        Quota = quota;
        AvailableUntil = availableUntil;
        Reserved = reserved;
    }

    public static CategoryPrice Create(TicketCategory category, decimal salePrice, int? quota,
        DateOnly? availableUntil = null, int? tierId = null)
    {
        if (salePrice < 0) throw new DomainException("Verkaufspreis darf nicht negativ sein.");
        if (quota is < 0) throw new DomainException("Kontingent darf nicht negativ sein.");
        return new CategoryPrice(category, tierId, decimal.Round(salePrice, 2), quota, availableUntil, 0);
    }

    public static CategoryPrice FromPersistence(TicketCategory category, decimal salePrice, int? quota,
        DateOnly? availableUntil = null, int? tierId = null, int reserved = 0) =>
        new(category, tierId, salePrice, quota, availableUntil, reserved);

    public bool IsOnSale(DateOnly today) => AvailableUntil is null || today <= AvailableUntil.Value;

    public int? Remaining(int sold) => Quota is { } q ? Math.Max(0, q - sold - Reserved) : null;

    internal void Reserve(int quantity) => Reserved += quantity;

    internal void Release(int quantity) => Reserved = Math.Max(0, Reserved - quantity);
}

public sealed class EventPrice
{
    public int Id { get; private set; }
    public int EventId { get; private set; }
    public int? TotalSalesQuota { get; private set; }
    public int? AdmissionQuota { get; private set; }
    public bool ConversionOnly { get; private set; }
    public IReadOnlyList<CategoryPrice> Categories { get; private set; }
    public int Reserved { get; private set; }
    public int Version { get; private set; }

    private EventPrice(int id, int eventId, int? totalSalesQuota, int? admissionQuota, bool conversionOnly,
        IReadOnlyList<CategoryPrice> categories, int reserved, int version)
    {
        Id = id;
        EventId = eventId;
        TotalSalesQuota = totalSalesQuota;
        AdmissionQuota = admissionQuota;
        ConversionOnly = conversionOnly;
        Categories = categories;
        Reserved = reserved;
        Version = version;
    }

    public const string SalesAboveAdmission = "Das Verkaufskontingent darf das Einlasskontingent nicht übersteigen.";

    public static EventPrice Create(int eventId, int? totalSalesQuota, int? admissionQuota, IReadOnlyList<CategoryPrice> categories,
        bool conversionOnly = false)
    {
        if (eventId <= 0) throw new DomainException("Ein Anlass muss zugewiesen sein.");
        if (totalSalesQuota is < 0) throw new DomainException("Verkaufskontingent darf nicht negativ sein.");
        if (admissionQuota is < 0) throw new DomainException("Einlasskontingent darf nicht negativ sein.");
        RequireSalesWithinAdmission(totalSalesQuota, admissionQuota);
        return new EventPrice(0, eventId, totalSalesQuota, admissionQuota, conversionOnly, categories ?? [], 0, 0);
    }

    public static EventPrice FromPersistence(int id, int eventId, int? totalSalesQuota, int? admissionQuota,
        IReadOnlyList<CategoryPrice> categories, bool conversionOnly = false, int reserved = 0, int version = 0) =>
        new(id, eventId, totalSalesQuota, admissionQuota, conversionOnly, categories ?? [], reserved, version);

    public EventPrice WithSalesQuota(int? totalSalesQuota)
    {
        if (totalSalesQuota is < 0) throw new DomainException("Verkaufskontingent darf nicht negativ sein.");
        RequireSalesWithinAdmission(totalSalesQuota, AdmissionQuota);
        return new EventPrice(Id, EventId, totalSalesQuota, AdmissionQuota, ConversionOnly, Categories, Reserved, Version);
    }

    public EventPrice WithAdmissionQuota(int? admissionQuota)
    {
        if (admissionQuota is < 0) throw new DomainException("Einlasskontingent darf nicht negativ sein.");
        RequireSalesWithinAdmission(TotalSalesQuota, admissionQuota);
        return new EventPrice(Id, EventId, TotalSalesQuota, admissionQuota, ConversionOnly, Categories, Reserved, Version);
    }

    private static void RequireSalesWithinAdmission(int? totalSalesQuota, int? admissionQuota)
    {
        if (totalSalesQuota is { } sales && admissionQuota is { } admission && sales > admission)
            throw new DomainException(SalesAboveAdmission);
    }

    public EventPrice WithConversionOnly(bool conversionOnly) =>
        new(Id, EventId, TotalSalesQuota, AdmissionQuota, conversionOnly, Categories, Reserved, Version);

    public EventPrice WithCategories(IReadOnlyList<CategoryPrice> categories) =>
        new(Id, EventId, TotalSalesQuota, AdmissionQuota, ConversionOnly, categories ?? [], Reserved, Version);

    public int? RemainingTotal(CapacityUsage usage) =>
        TotalSalesQuota is { } q ? Math.Max(0, q - usage.SoldTotal - Reserved) : null;

    public CheckResult Reserve(IReadOnlyList<TierDemand> demand, CapacityUsage usage, DateOnly today)
    {
        var requestedTotal = demand.Sum(d => d.Quantity);
        if (requestedTotal <= 0) return CheckResult.Allow();
        if (RemainingTotal(usage) is { } remaining && remaining < requestedTotal)
            return CheckResult.Deny(new CapacityDenied.TotalExhausted());

        var perTier = new List<(CategoryPrice Category, int Quantity)>();
        foreach (var group in demand.Where(d => !d.IsConversion).GroupBy(d => d.TierId))
        {
            var category = Categories.FirstOrDefault(c => c.TierId == group.Key);
            if (category is null || !category.IsOnSale(today))
                return CheckResult.Deny(new CapacityDenied.TierUnavailable(group.Key));
            var requested = group.Sum(d => d.Quantity);
            if (category.Remaining(usage.SoldFor(group.Key)) is { } left && left < requested)
                return CheckResult.Deny(new CapacityDenied.TierExhausted(group.Key, left));
            perTier.Add((category, requested));
        }

        Reserved += requestedTotal;
        foreach (var (category, quantity) in perTier) category.Reserve(quantity);
        return CheckResult.Allow();
    }

    public void Release(IReadOnlyList<TierDemand> demand)
    {
        Reserved = Math.Max(0, Reserved - demand.Sum(d => d.Quantity));
        foreach (var group in demand.Where(d => !d.IsConversion).GroupBy(d => d.TierId))
            Categories.FirstOrDefault(c => c.TierId == group.Key)?.Release(group.Sum(d => d.Quantity));
    }
}

public sealed class SeasonCategoryPrice
{
    public TicketCategory Category { get; private set; }
    public int? TierId { get; private set; }
    public decimal PassPrice { get; private set; }
    public bool PassOffered { get; private set; }
    public int? PassQuota { get; private set; }
    public DateOnly? PassAvailableFrom { get; private set; }
    public DateOnly? PassAvailableUntil { get; private set; }
    public decimal TicketPrice { get; private set; }
    public bool TicketOffered { get; private set; }
    public int? TicketQuota { get; private set; }
    public DateOnly? TicketAvailableUntil { get; private set; }
    public int Reserved { get; private set; }

    private SeasonCategoryPrice(TicketCategory category, int? tierId, decimal passPrice, bool passOffered, int? passQuota,
        DateOnly? passAvailableFrom, DateOnly? passAvailableUntil, decimal ticketPrice, bool ticketOffered, int? ticketQuota,
        DateOnly? ticketAvailableUntil, int reserved)
    {
        Category = category;
        TierId = tierId;
        PassPrice = passPrice;
        PassOffered = passOffered;
        PassQuota = passQuota;
        PassAvailableFrom = passAvailableFrom;
        PassAvailableUntil = passAvailableUntil;
        TicketPrice = ticketPrice;
        TicketOffered = ticketOffered;
        TicketQuota = ticketQuota;
        TicketAvailableUntil = ticketAvailableUntil;
        Reserved = reserved;
    }

    public static SeasonCategoryPrice Create(TicketCategory category, decimal passPrice, bool passOffered, int? passQuota,
        decimal ticketPrice, bool ticketOffered, int? ticketQuota,
        DateOnly? passAvailableFrom = null, DateOnly? passAvailableUntil = null, DateOnly? ticketAvailableUntil = null, int? tierId = null)
    {
        if (passPrice < 0) throw new DomainException("Saisonkarten-Preis darf nicht negativ sein.");
        if (ticketPrice < 0) throw new DomainException("Ticketpreis darf nicht negativ sein.");
        if (passQuota is < 0 || ticketQuota is < 0) throw new DomainException("Kontingent darf nicht negativ sein.");
        if (passAvailableFrom is { } f && passAvailableUntil is { } u && f > u)
            throw new DomainException("Verkauf von darf nicht nach Verkauf bis liegen.");
        return new SeasonCategoryPrice(category, tierId, decimal.Round(passPrice, 2), passOffered, passQuota, passAvailableFrom, passAvailableUntil,
            decimal.Round(ticketPrice, 2), ticketOffered, ticketQuota, ticketAvailableUntil, 0);
    }

    public static SeasonCategoryPrice FromPersistence(TicketCategory category, decimal passPrice, bool passOffered,
        int? passQuota, decimal ticketPrice, bool ticketOffered, int? ticketQuota,
        DateOnly? passAvailableFrom = null, DateOnly? passAvailableUntil = null, DateOnly? ticketAvailableUntil = null, int? tierId = null,
        int reserved = 0) =>
        new(category, tierId, passPrice, passOffered, passQuota, passAvailableFrom, passAvailableUntil,
            ticketPrice, ticketOffered, ticketQuota, ticketAvailableUntil, reserved);

    public bool IsPassOnSale(DateOnly today) =>
        PassOffered
        && (PassAvailableFrom is null || today >= PassAvailableFrom.Value)
        && (PassAvailableUntil is null || today <= PassAvailableUntil.Value);

    public int? RemainingPasses(int sold) => PassQuota is { } q ? Math.Max(0, q - sold - Reserved) : null;

    internal void Reserve(int quantity) => Reserved += quantity;

    internal void Release(int quantity) => Reserved = Math.Max(0, Reserved - quantity);
}

public sealed class SeasonPrice
{
    public int Id { get; private set; }
    public int SeasonId { get; private set; }
    public int? TotalSalesQuota { get; private set; }
    public int? DefaultTicketSalesQuota { get; private set; }
    public IReadOnlyList<SeasonCategoryPrice> Categories { get; private set; }
    public int Reserved { get; private set; }
    public int Version { get; private set; }

    private SeasonPrice(int id, int seasonId, int? totalSalesQuota, int? defaultTicketSalesQuota,
        IReadOnlyList<SeasonCategoryPrice> categories, int reserved, int version)
    {
        Id = id;
        SeasonId = seasonId;
        TotalSalesQuota = totalSalesQuota;
        DefaultTicketSalesQuota = defaultTicketSalesQuota;
        Categories = categories;
        Reserved = reserved;
        Version = version;
    }

    public static SeasonPrice Create(int seasonId, int? totalSalesQuota, IReadOnlyList<SeasonCategoryPrice> categories, int? defaultTicketSalesQuota = null)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
        if (totalSalesQuota is < 0) throw new DomainException("Einlasskontingent darf nicht negativ sein.");
        if (defaultTicketSalesQuota is < 0) throw new DomainException("Verkaufskontingent darf nicht negativ sein.");
        return new SeasonPrice(0, seasonId, totalSalesQuota, defaultTicketSalesQuota, categories ?? [], 0, 0);
    }

    public static SeasonPrice FromPersistence(int id, int seasonId, int? totalSalesQuota, IReadOnlyList<SeasonCategoryPrice> categories,
        int? defaultTicketSalesQuota = null, int reserved = 0, int version = 0) =>
        new(id, seasonId, totalSalesQuota, defaultTicketSalesQuota, categories ?? [], reserved, version);

    public SeasonPrice WithTotalSalesQuota(int? totalSalesQuota)
    {
        if (totalSalesQuota is < 0) throw new DomainException("Einlasskontingent darf nicht negativ sein.");
        return new SeasonPrice(Id, SeasonId, totalSalesQuota, DefaultTicketSalesQuota, Categories, Reserved, Version);
    }

    public SeasonPrice WithDefaultTicketSalesQuota(int? defaultTicketSalesQuota)
    {
        if (defaultTicketSalesQuota is < 0) throw new DomainException("Verkaufskontingent darf nicht negativ sein.");
        return new SeasonPrice(Id, SeasonId, TotalSalesQuota, defaultTicketSalesQuota, Categories, Reserved, Version);
    }

    public SeasonPrice WithCategories(IReadOnlyList<SeasonCategoryPrice> categories) =>
        new(Id, SeasonId, TotalSalesQuota, DefaultTicketSalesQuota, categories ?? [], Reserved, Version);

    public int? RemainingTotal(CapacityUsage usage) =>
        TotalSalesQuota is { } q ? Math.Max(0, q - usage.SoldTotal - Reserved) : null;

    public CheckResult ReservePasses(IReadOnlyList<TierDemand> demand, CapacityUsage usage, DateOnly today)
    {
        var requestedTotal = demand.Sum(d => d.Quantity);
        if (requestedTotal <= 0) return CheckResult.Allow();
        if (RemainingTotal(usage) is { } remaining && remaining < requestedTotal)
            return CheckResult.Deny(new CapacityDenied.TotalExhausted());

        var perTier = new List<(SeasonCategoryPrice Category, int Quantity)>();
        foreach (var group in demand.GroupBy(d => d.TierId))
        {
            var category = Categories.FirstOrDefault(c => c.TierId == group.Key);
            if (category is null || !category.IsPassOnSale(today))
                return CheckResult.Deny(new CapacityDenied.TierUnavailable(group.Key));
            var requested = group.Sum(d => d.Quantity);
            if (category.RemainingPasses(usage.SoldFor(group.Key)) is { } left && left < requested)
                return CheckResult.Deny(new CapacityDenied.TierExhausted(group.Key, left));
            perTier.Add((category, requested));
        }

        Reserved += requestedTotal;
        foreach (var (category, quantity) in perTier) category.Reserve(quantity);
        return CheckResult.Allow();
    }

    public void ReleasePasses(IReadOnlyList<TierDemand> demand)
    {
        Reserved = Math.Max(0, Reserved - demand.Sum(d => d.Quantity));
        foreach (var group in demand.GroupBy(d => d.TierId))
            Categories.FirstOrDefault(c => c.TierId == group.Key)?.Release(group.Sum(d => d.Quantity));
    }
}
