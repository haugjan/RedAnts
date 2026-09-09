namespace RedAnts.Ticketing.Domain.Sales;

public sealed class FlexTicketBundle
{
    public const int ReferenceMaxLength = 50;

    public int Id { get; private set; }
    public int SeasonId { get; private set; }
    public TicketCategory Category { get; private set; }
    public string Reference { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string? CreatedByName { get; private set; }
    public string? CreatedByEmail { get; private set; }

    private FlexTicketBundle(int id, int seasonId, TicketCategory category, string reference,
        DateTimeOffset createdAt, string? createdByName, string? createdByEmail)
    {
        Id = id;
        SeasonId = seasonId;
        Category = category;
        Reference = reference;
        CreatedAt = createdAt;
        CreatedByName = createdByName;
        CreatedByEmail = createdByEmail;
    }

    public static FlexTicketBundle Create(int seasonId, TicketCategory category, string reference,
        string? createdByName = null, string? createdByEmail = null, TimeProvider? time = null)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
        return new FlexTicketBundle(0, seasonId, category, CleanReference(reference), SwissTime.TimestampOf(time),
            Clean(createdByName), Clean(createdByEmail));
    }

    public static FlexTicketBundle FromPersistence(int id, int seasonId, TicketCategory category,
        string reference, DateTimeOffset createdAt, string? createdByName = null, string? createdByEmail = null) =>
        new(id, seasonId, category, reference, createdAt, Clean(createdByName), Clean(createdByEmail));

    public void Rename(string reference) => Reference = CleanReference(reference);

    private static string CleanReference(string? reference)
    {
        var value = (reference ?? "").Trim();
        if (value.Length == 0) throw new DomainException("Ein Bundle muss angegeben werden.");
        if (value.Length > ReferenceMaxLength)
            throw new DomainException($"Das Bundle darf höchstens {ReferenceMaxLength} Zeichen lang sein.");
        return value;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
