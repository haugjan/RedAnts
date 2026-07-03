namespace RedAnts.Domain.Ticketing.Sales;

/// <summary>One recorded admission of a multi-event ticket (SeasonPass / MemberCard) at a specific event.
/// A new row is created on check-in; check-out sets <see cref="CheckedOutAt"/>. Current presence at an
/// event = the latest row for (TicketUuid, EventId) has no CheckedOutAt.</summary>
public sealed class EventVisit
{
    public long Id { get; private set; }
    public TicketType TicketType { get; private set; }
    public Guid TicketUuid { get; private set; }
    public int EventId { get; private set; }
    public DateTime CheckedInAt { get; private set; }
    public DateTime? CheckedOutAt { get; private set; }
    public string? ScannedBy { get; private set; }

    public bool IsInside => CheckedOutAt is null;

    private EventVisit(long id, TicketType ticketType, Guid ticketUuid, int eventId, DateTime checkedInAt,
        DateTime? checkedOutAt, string? scannedBy)
    {
        Id = id;
        TicketType = ticketType;
        TicketUuid = ticketUuid;
        EventId = eventId;
        CheckedInAt = checkedInAt;
        CheckedOutAt = checkedOutAt;
        ScannedBy = scannedBy;
    }

    public static EventVisit CheckIn(TicketType ticketType, Guid ticketUuid, int eventId, string? scannedBy)
    {
        if (eventId <= 0) throw new DomainException("Ein Anlass muss zugewiesen sein.");
        return new EventVisit(0, ticketType, ticketUuid, eventId, DateTime.UtcNow, null, scannedBy);
    }

    public static EventVisit FromPersistence(long id, TicketType ticketType, Guid ticketUuid, int eventId,
        DateTime checkedInAt, DateTime? checkedOutAt, string? scannedBy) =>
        new(id, ticketType, ticketUuid, eventId, checkedInAt, checkedOutAt, scannedBy);

    /// <summary>Scan-out (Auscannen): the holder leaves the building; re-entry creates a new visit row.</summary>
    public void CheckOut()
    {
        if (!IsInside) throw new DomainException("Besuch ist bereits abgeschlossen.");
        CheckedOutAt = DateTime.UtcNow;
    }
}
