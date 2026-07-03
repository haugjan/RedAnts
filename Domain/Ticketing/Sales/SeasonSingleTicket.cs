namespace RedAnts.Domain.Ticketing.Sales;

/// <summary>A single-admission ticket bound to a season but not to a specific event: usable once at any
/// one event of the season. The first check-in binds it to that event; afterwards it is only valid there.</summary>
public sealed class SeasonSingleTicket
{
    public int Id { get; private set; }
    public Guid Uuid { get; private set; }
    public int SeasonId { get; private set; }
    public string CategoryCode { get; private set; }
    public string CategoryName { get; private set; }
    public decimal Price { get; private set; }
    public int? OrderId { get; private set; }
    public TicketStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    /// <summary>The event this ticket was redeemed at (set on first check-in); null while unredeemed.</summary>
    public int? RedeemedEventId { get; private set; }
    public DateTime? CheckedInAt { get; private set; }
    public DateTime? RedeemedAt { get; private set; }
    public string? RedeemedBy { get; private set; }

    public bool IsInside => CheckedInAt is not null;
    public bool IsRedeemed => RedeemedEventId is not null;

    private SeasonSingleTicket(int id, Guid uuid, int seasonId, string categoryCode, string categoryName, decimal price,
        int? orderId, TicketStatus status, DateTime createdAt, int? redeemedEventId, DateTime? checkedInAt,
        DateTime? redeemedAt, string? redeemedBy)
    {
        Id = id;
        Uuid = uuid;
        SeasonId = seasonId;
        CategoryCode = categoryCode;
        CategoryName = categoryName;
        Price = price;
        OrderId = orderId;
        Status = status;
        CreatedAt = createdAt;
        RedeemedEventId = redeemedEventId;
        CheckedInAt = checkedInAt;
        RedeemedAt = redeemedAt;
        RedeemedBy = redeemedBy;
    }

    public static SeasonSingleTicket Create(int seasonId, string categoryCode, string categoryName, decimal price, int? orderId)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
        if (price < 0) throw new DomainException("Preis darf nicht negativ sein.");
        return new SeasonSingleTicket(0, Guid.NewGuid(), seasonId, categoryCode ?? "", categoryName ?? "",
            decimal.Round(price, 2), orderId, TicketStatus.Valid, DateTime.UtcNow, null, null, null, null);
    }

    public static SeasonSingleTicket FromPersistence(int id, Guid uuid, int seasonId, string categoryCode,
        string categoryName, decimal price, int? orderId, TicketStatus status, DateTime createdAt, int? redeemedEventId,
        DateTime? checkedInAt, DateTime? redeemedAt, string? redeemedBy) =>
        new(id, uuid, seasonId, categoryCode ?? "", categoryName ?? "", price, orderId, status, createdAt,
            redeemedEventId, checkedInAt, redeemedAt, redeemedBy);

    /// <summary>Scan-in at an event of the season. The first check-in consumes the ticket at that event.</summary>
    public void CheckIn(int eventId, string? scannedBy)
    {
        if (Status != TicketStatus.Valid) throw new DomainException("Ticket ist ungültig.");
        if (IsRedeemed && RedeemedEventId != eventId)
            throw new DomainException("Ticket wurde bereits an einem anderen Anlass eingelöst.");
        if (IsInside) throw new DomainException("Ticket ist bereits eingecheckt.");
        RedeemedEventId ??= eventId;
        CheckedInAt = DateTime.UtcNow;
        RedeemedAt ??= CheckedInAt;
        RedeemedBy = scannedBy;
    }

    /// <summary>Scan-out (Auscannen). Stays bound to its event; re-entry there is possible.</summary>
    public void CheckOut()
    {
        if (!IsInside) throw new DomainException("Ticket ist nicht eingecheckt.");
        CheckedInAt = null;
    }
}
