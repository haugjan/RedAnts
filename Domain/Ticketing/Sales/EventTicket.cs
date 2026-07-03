namespace RedAnts.Domain.Ticketing.Sales;

/// <summary>A ticket for one specific event. Scannable, single admission with in/out at that event.</summary>
public sealed class EventTicket
{
    public int Id { get; private set; }
    public Guid Uuid { get; private set; }
    public int EventId { get; private set; }
    public string CategoryCode { get; private set; }
    public string CategoryName { get; private set; }
    public decimal Price { get; private set; }
    public int? OrderId { get; private set; }
    public TicketStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CheckedInAt { get; private set; }
    public DateTime? RedeemedAt { get; private set; }
    public string? RedeemedBy { get; private set; }

    public bool IsInside => CheckedInAt is not null;

    private EventTicket(int id, Guid uuid, int eventId, string categoryCode, string categoryName, decimal price,
        int? orderId, TicketStatus status, DateTime createdAt, DateTime? checkedInAt, DateTime? redeemedAt, string? redeemedBy)
    {
        Id = id;
        Uuid = uuid;
        EventId = eventId;
        CategoryCode = categoryCode;
        CategoryName = categoryName;
        Price = price;
        OrderId = orderId;
        Status = status;
        CreatedAt = createdAt;
        CheckedInAt = checkedInAt;
        RedeemedAt = redeemedAt;
        RedeemedBy = redeemedBy;
    }

    public static EventTicket Create(int eventId, string categoryCode, string categoryName, decimal price, int? orderId)
    {
        if (eventId <= 0) throw new DomainException("Ein Anlass muss zugewiesen sein.");
        if (price < 0) throw new DomainException("Preis darf nicht negativ sein.");
        return new EventTicket(0, Guid.NewGuid(), eventId, categoryCode ?? "", categoryName ?? "",
            decimal.Round(price, 2), orderId, TicketStatus.Valid, DateTime.UtcNow, null, null, null);
    }

    public static EventTicket FromPersistence(int id, Guid uuid, int eventId, string categoryCode, string categoryName,
        decimal price, int? orderId, TicketStatus status, DateTime createdAt, DateTime? checkedInAt,
        DateTime? redeemedAt, string? redeemedBy) =>
        new(id, uuid, eventId, categoryCode ?? "", categoryName ?? "", price, orderId, status, createdAt,
            checkedInAt, redeemedAt, redeemedBy);

    /// <summary>Scan-in at the ticket's event.</summary>
    public void CheckIn(int eventId, string? scannedBy)
    {
        if (Status != TicketStatus.Valid) throw new DomainException("Ticket ist ungültig.");
        if (eventId != EventId) throw new DomainException("Ticket gilt nicht für diesen Anlass.");
        if (IsInside) throw new DomainException("Ticket ist bereits eingecheckt.");
        CheckedInAt = DateTime.UtcNow;
        RedeemedAt ??= CheckedInAt;
        RedeemedBy = scannedBy;
    }

    /// <summary>Scan-out (Auscannen). The ticket is available again for re-entry at the same event.</summary>
    public void CheckOut()
    {
        if (!IsInside) throw new DomainException("Ticket ist nicht eingecheckt.");
        CheckedInAt = null;
    }
}
