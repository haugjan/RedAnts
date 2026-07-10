namespace RedAnts.Domain.Ticketing.Sales;

public sealed class EventTicket
{
    public int Id { get; private set; }
    public Guid Uuid { get; private set; }
    public int EventId { get; private set; }
    public TicketCategory Category { get; private set; }
    public decimal Price { get; private set; }
    public int? OrderId { get; private set; }
    public TicketStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool Redeemed { get; private set; }
    public Buyer? Buyer { get; private set; }
    public string? CreatedByName { get; private set; }
    public string? CreatedByEmail { get; private set; }
    public int? BundleId { get; private set; }

    private EventTicket(int id, Guid uuid, int eventId, TicketCategory category, decimal price,
        int? orderId, TicketStatus status, DateTime createdAt, bool redeemed, Buyer? buyer,
        string? createdByName, string? createdByEmail, int? bundleId)
    {
        Id = id;
        Uuid = uuid;
        EventId = eventId;
        Category = category;
        Price = price;
        OrderId = orderId;
        Status = status;
        CreatedAt = createdAt;
        Redeemed = redeemed;
        Buyer = buyer;
        CreatedByName = createdByName;
        CreatedByEmail = createdByEmail;
        BundleId = bundleId;
    }

    public static EventTicket Create(int eventId, TicketCategory category, decimal price, int? orderId,
        Buyer? buyer = null, string? createdByName = null, string? createdByEmail = null)
    {
        if (eventId <= 0) throw new DomainException("Ein Anlass muss zugewiesen sein.");
        if (price < 0) throw new DomainException("Preis darf nicht negativ sein.");
        return new EventTicket(0, Guid.NewGuid(), eventId, category, decimal.Round(price, 2),
            orderId, TicketStatus.Valid, DateTime.UtcNow, false, buyer, Clean(createdByName), Clean(createdByEmail), null);
    }

    public static EventTicket CreateForBundle(int eventId, TicketCategory category, int bundleId,
        string? createdByName = null, string? createdByEmail = null)
    {
        if (eventId <= 0) throw new DomainException("Ein Anlass muss zugewiesen sein.");
        if (bundleId <= 0) throw new DomainException("Ein Bundle muss zugewiesen sein.");
        return new EventTicket(0, Guid.NewGuid(), eventId, category, 0m, null,
            TicketStatus.Valid, DateTime.UtcNow, false, null, Clean(createdByName), Clean(createdByEmail), bundleId);
    }

    public static EventTicket FromPersistence(int id, Guid uuid, int eventId, TicketCategory category,
        decimal price, int? orderId, TicketStatus status, DateTime createdAt, bool redeemed,
        Buyer? buyer = null, string? createdByName = null, string? createdByEmail = null, int? bundleId = null) =>
        new(id, uuid, eventId, category, price, orderId, status, createdAt, redeemed, buyer,
            Clean(createdByName), Clean(createdByEmail), bundleId);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public void Redeem()
    {
        if (Status != TicketStatus.Valid) throw new DomainException("Ticket ist ungültig.");
        Redeemed = true;
    }
}
