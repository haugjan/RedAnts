namespace RedAnts.Domain.Ticketing.Sales;

public sealed class EventTicketBundle
{
    public const int ReferenceMaxLength = 50;

    public int Id { get; private set; }
    public int EventId { get; private set; }
    public TicketCategory Category { get; private set; }
    public string Reference { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string? CreatedByName { get; private set; }
    public string? CreatedByEmail { get; private set; }

    private EventTicketBundle(int id, int eventId, TicketCategory category, string reference,
        DateTime createdAt, string? createdByName, string? createdByEmail)
    {
        Id = id;
        EventId = eventId;
        Category = category;
        Reference = reference;
        CreatedAt = createdAt;
        CreatedByName = createdByName;
        CreatedByEmail = createdByEmail;
    }

    public static EventTicketBundle Create(int eventId, TicketCategory category, string reference,
        string? createdByName = null, string? createdByEmail = null)
    {
        if (eventId <= 0) throw new DomainException("Ein Anlass muss zugewiesen sein.");
        reference = (reference ?? "").Trim();
        if (reference.Length == 0) throw new DomainException("Ein Bundlename muss angegeben werden.");
        if (reference.Length > ReferenceMaxLength)
            throw new DomainException($"Der Bundlename darf höchstens {ReferenceMaxLength} Zeichen lang sein.");
        return new EventTicketBundle(0, eventId, category, reference, DateTime.UtcNow,
            Clean(createdByName), Clean(createdByEmail));
    }

    public static EventTicketBundle FromPersistence(int id, int eventId, TicketCategory category,
        string reference, DateTime createdAt, string? createdByName = null, string? createdByEmail = null) =>
        new(id, eventId, category, reference, createdAt, Clean(createdByName), Clean(createdByEmail));

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
