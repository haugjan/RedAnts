namespace RedAnts.Domain.Ticketing.Sales;

/// <summary>A season pass (Saisonkarte): valid at every event of its season. Per-event admissions are
/// tracked as TicketEventVisits rows (one per event), so a holder can enter each event independently.</summary>
public sealed class SeasonPass
{
    public int Id { get; private set; }
    public Guid Uuid { get; private set; }
    public int SeasonId { get; private set; }
    public TicketCategory Category { get; private set; }
    public decimal Price { get; private set; }
    public int? OrderId { get; private set; }
    public TicketStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private SeasonPass(int id, Guid uuid, int seasonId, TicketCategory category, decimal price,
        int? orderId, TicketStatus status, DateTime createdAt)
    {
        Id = id;
        Uuid = uuid;
        SeasonId = seasonId;
        Category = category;
        Price = price;
        OrderId = orderId;
        Status = status;
        CreatedAt = createdAt;
    }

    public static SeasonPass Create(int seasonId, TicketCategory category, decimal price, int? orderId)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
        if (price < 0) throw new DomainException("Preis darf nicht negativ sein.");
        return new SeasonPass(0, Guid.NewGuid(), seasonId, category, decimal.Round(price, 2),
            orderId, TicketStatus.Valid, DateTime.UtcNow);
    }

    public static SeasonPass FromPersistence(int id, Guid uuid, int seasonId, TicketCategory category,
        decimal price, int? orderId, TicketStatus status, DateTime createdAt) =>
        new(id, uuid, seasonId, category, price, orderId, status, createdAt);

    /// <summary>Admin correction of the editable fields on an already issued pass
    /// (category, price and validity). The season, order link and issue date stay fixed.</summary>
    public void Edit(TicketCategory category, decimal price, TicketStatus status)
    {
        if (price < 0) throw new DomainException("Preis darf nicht negativ sein.");
        Category = category;
        Price = decimal.Round(price, 2);
        Status = status;
    }
}
