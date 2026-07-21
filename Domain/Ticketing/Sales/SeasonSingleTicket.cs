namespace RedAnts.Domain.Ticketing.Sales;

public sealed class SeasonSingleTicket
{
    public int Id { get; private set; }
    public Guid Uuid { get; private set; }
    public int SeasonId { get; private set; }
    public TicketCategory Category { get; private set; }
    public int? TierId { get; private set; }
    public decimal Price { get; private set; }
    public int? OrderId { get; private set; }
    public TicketStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public int? RedeemedEventId { get; private set; }
    public bool Redeemed { get; private set; }
    public int? BundleId { get; private set; }

    private SeasonSingleTicket(int id, Guid uuid, int seasonId, TicketCategory category, int? tierId, decimal price,
        int? orderId, TicketStatus status, DateTime createdAt, int? redeemedEventId, bool redeemed,
        int? bundleId = null)
    {
        Id = id;
        Uuid = uuid;
        SeasonId = seasonId;
        Category = category;
        TierId = tierId;
        Price = price;
        OrderId = orderId;
        Status = status;
        CreatedAt = createdAt;
        RedeemedEventId = redeemedEventId;
        Redeemed = redeemed;
        BundleId = bundleId;
    }

    public static SeasonSingleTicket Create(int seasonId, TicketCategory category, decimal price, int? orderId,
        int? tierId = null)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
        if (price < 0) throw new DomainException("Preis darf nicht negativ sein.");
        return new SeasonSingleTicket(0, Guid.NewGuid(), seasonId, category, tierId, decimal.Round(price, 2),
            orderId, TicketStatus.Valid, DateTime.UtcNow, null, false);
    }

    public static SeasonSingleTicket CreateForBundle(int seasonId, TicketCategory category, decimal price, int bundleId,
        int? tierId = null, int? orderId = null)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
        if (price < 0) throw new DomainException("Preis darf nicht negativ sein.");
        if (bundleId <= 0) throw new DomainException("Ein Bundle muss zugewiesen sein.");
        return new SeasonSingleTicket(0, Guid.NewGuid(), seasonId, category, tierId, decimal.Round(price, 2),
            orderId, TicketStatus.Valid, DateTime.UtcNow, null, false, bundleId);
    }

    public static SeasonSingleTicket FromPersistence(int id, Guid uuid, int seasonId, TicketCategory category,
        decimal price, int? orderId, TicketStatus status, DateTime createdAt, int? redeemedEventId, bool redeemed,
        int? bundleId = null, int? tierId = null) =>
        new(id, uuid, seasonId, category, tierId, price, orderId, status, createdAt, redeemedEventId, redeemed, bundleId);

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
