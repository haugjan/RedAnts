namespace RedAnts.Domain.Ticketing.Sales;

public sealed class SeasonAddOn
{
    public int Id { get; private set; }
    public int SeasonId { get; private set; }
    public string Label { get; private set; }
    public decimal Price { get; private set; }
    public bool Active { get; private set; }
    public int SortOrder { get; private set; }

    private SeasonAddOn(int id, int seasonId, string label, decimal price, bool active, int sortOrder)
    {
        Id = id;
        SeasonId = seasonId;
        Label = label;
        Price = price;
        Active = active;
        SortOrder = sortOrder;
    }

    public static SeasonAddOn Create(int seasonId, string label, decimal price, bool active, int sortOrder)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
        var trimmed = (label ?? "").Trim();
        if (trimmed.Length == 0) throw new DomainException("Die Zusatzoption braucht eine Bezeichnung.");
        if (price < 0) throw new DomainException("Der Preis darf nicht negativ sein.");
        return new SeasonAddOn(0, seasonId, trimmed, decimal.Round(price, 2), active, sortOrder);
    }

    public static SeasonAddOn FromPersistence(int id, int seasonId, string label, decimal price, bool active, int sortOrder) =>
        new(id, seasonId, label, price, active, sortOrder);
}
