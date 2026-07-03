namespace RedAnts.Domain.Ticketing.Sales;

/// <summary>A single-admission ticket bound to a season but not to a specific event: usable once at any
/// one event of the season. The first check-in binds it to that event (<see cref="RedeemedEventId"/>)
/// and marks it redeemed. Admission in/out is tracked in TicketEventVisits.</summary>
public sealed class SeasonSingleTicket
{
    public int Id { get; private set; }
    public Guid Uuid { get; private set; }
    public int SeasonId { get; private set; }
    public TicketCategory Category { get; private set; }
    public decimal Price { get; private set; }
    public int? OrderId { get; private set; }
    public TicketStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    /// <summary>The event this ticket was redeemed at (set on first check-in); null while unredeemed.</summary>
    public int? RedeemedEventId { get; private set; }
    /// <summary>True once the ticket has been consumed at its event.</summary>
    public bool Redeemed { get; private set; }

    private SeasonSingleTicket(int id, Guid uuid, int seasonId, TicketCategory category, decimal price,
        int? orderId, TicketStatus status, DateTime createdAt, int? redeemedEventId, bool redeemed)
    {
        Id = id;
        Uuid = uuid;
        SeasonId = seasonId;
        Category = category;
        Price = price;
        OrderId = orderId;
        Status = status;
        CreatedAt = createdAt;
        RedeemedEventId = redeemedEventId;
        Redeemed = redeemed;
    }

    public static SeasonSingleTicket Create(int seasonId, TicketCategory category, decimal price, int? orderId)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
        if (price < 0) throw new DomainException("Preis darf nicht negativ sein.");
        return new SeasonSingleTicket(0, Guid.NewGuid(), seasonId, category, decimal.Round(price, 2),
            orderId, TicketStatus.Valid, DateTime.UtcNow, null, false);
    }

    public static SeasonSingleTicket FromPersistence(int id, Guid uuid, int seasonId, TicketCategory category,
        decimal price, int? orderId, TicketStatus status, DateTime createdAt, int? redeemedEventId, bool redeemed) =>
        new(id, uuid, seasonId, category, price, orderId, status, createdAt, redeemedEventId, redeemed);

    /// <summary>Consume the ticket at an event of the season (first check-in). Binds it to that event.</summary>
    public void Redeem(int eventId)
    {
        if (Status != TicketStatus.Valid) throw new DomainException("Ticket ist ungültig.");
        if (eventId <= 0) throw new DomainException("Ein Anlass muss zugewiesen sein.");
        if (Redeemed && RedeemedEventId != eventId)
            throw new DomainException("Ticket wurde bereits an einem anderen Anlass eingelöst.");
        RedeemedEventId = eventId;
        Redeemed = true;
    }
}
