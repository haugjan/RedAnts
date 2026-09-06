namespace RedAnts.Domain.Ticketing.Sales;

public sealed class EventVisit
{
    public long Id { get; private set; }
    public int EventId { get; private set; }
    public TicketType TicketType { get; private set; }
    public Guid? TicketUuid { get; private set; }
    public bool IsInside { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private EventVisit(long id, int eventId, TicketType ticketType, Guid? ticketUuid, bool isInside, DateTime createdAt)
    {
        Id = id;
        EventId = eventId;
        TicketType = ticketType;
        TicketUuid = ticketUuid;
        IsInside = isInside;
        CreatedAt = createdAt;
    }

    public static EventVisit CreateForTicket(int eventId, TicketType ticketType, Guid ticketUuid)
    {
        if (eventId <= 0) throw new DomainException("Ein Anlass muss zugewiesen sein.");
        if (ticketType == TicketType.FreeEntry) throw new DomainException("Free-Entry hat kein Ticket.");
        return new EventVisit(0, eventId, ticketType, ticketUuid, isInside: true, DateTime.UtcNow);
    }

    public static EventVisit CreateForFreeEntry(int eventId)
    {
        if (eventId <= 0) throw new DomainException("Ein Anlass muss zugewiesen sein.");
        return new EventVisit(0, eventId, TicketType.FreeEntry, null, isInside: true, DateTime.UtcNow);
    }

    public static EventVisit FromPersistence(long id, int eventId, TicketType ticketType, Guid? ticketUuid,
        bool isInside, DateTime createdAt) =>
        new(id, eventId, ticketType, ticketUuid, isInside, createdAt);

    public void SetInside(bool inside) => IsInside = inside;
}

public sealed class EventVisitLog
{
    public long Id { get; private set; }
    public long VisitId { get; private set; }
    public VisitLogType Type { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public string? ScannedBy { get; private set; }

    private EventVisitLog(long id, long visitId, VisitLogType type, DateTime occurredAt, string? scannedBy)
    {
        Id = id;
        VisitId = visitId;
        Type = type;
        OccurredAt = occurredAt;
        ScannedBy = scannedBy;
    }

    public static EventVisitLog Create(long visitId, VisitLogType type, string? scannedBy) =>
        new(0, visitId, type, DateTime.UtcNow, scannedBy);

    public static EventVisitLog FromPersistence(long id, long visitId, VisitLogType type, DateTime occurredAt, string? scannedBy) =>
        new(id, visitId, type, occurredAt, scannedBy);
}

public sealed class EventFreeEntry
{
    public long Id { get; private set; }
    public long VisitId { get; private set; }
    public FreeEntryType Type { get; private set; }

    private EventFreeEntry(long id, long visitId, FreeEntryType type)
    {
        Id = id;
        VisitId = visitId;
        Type = type;
    }

    public static EventFreeEntry Create(long visitId, FreeEntryType type) => new(0, visitId, type);

    public static EventFreeEntry FromPersistence(long id, long visitId, FreeEntryType type) => new(id, visitId, type);
}
