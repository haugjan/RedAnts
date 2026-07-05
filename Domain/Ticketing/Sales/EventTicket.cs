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

    private EventTicket(int id, Guid uuid, int eventId, TicketCategory category, decimal price,
        int? orderId, TicketStatus status, DateTime createdAt, bool redeemed)
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
    }

    public static EventTicket Create(int eventId, TicketCategory category, decimal price, int? orderId)
    {
        if (eventId <= 0) throw new DomainException("Ein Anlass muss zugewiesen sein.");
        if (price < 0) throw new DomainException("Preis darf nicht negativ sein.");
        return new EventTicket(0, Guid.NewGuid(), eventId, category, decimal.Round(price, 2),
            orderId, TicketStatus.Valid, DateTime.UtcNow, false);
    }

    public static EventTicket FromPersistence(int id, Guid uuid, int eventId, TicketCategory category,
        decimal price, int? orderId, TicketStatus status, DateTime createdAt, bool redeemed) =>
        new(id, uuid, eventId, category, price, orderId, status, createdAt, redeemed);

    public void Redeem()
    {
        if (Status != TicketStatus.Valid) throw new DomainException("Ticket ist ungültig.");
        Redeemed = true;
    }
}
