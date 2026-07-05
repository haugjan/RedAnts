namespace RedAnts.Domain.Ticketing.Sales;

public sealed class FlexTicketBundle
{
    public const int ReferenceMaxLength = 50;

    public int Id { get; private set; }
    public int SeasonId { get; private set; }
    public TicketCategory Category { get; private set; }
    public string Reference { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string? CreatedByName { get; private set; }
    public string? CreatedByEmail { get; private set; }

    private FlexTicketBundle(int id, int seasonId, TicketCategory category, string reference,
        DateTime createdAt, string? createdByName, string? createdByEmail)
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
        string? createdByName = null, string? createdByEmail = null)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
        reference = (reference ?? "").Trim();
        if (reference.Length == 0) throw new DomainException("Eine Referenz muss angegeben werden.");
        if (reference.Length > ReferenceMaxLength)
            throw new DomainException($"Die Referenz darf höchstens {ReferenceMaxLength} Zeichen lang sein.");
        return new FlexTicketBundle(0, seasonId, category, reference, DateTime.UtcNow,
            Clean(createdByName), Clean(createdByEmail));
    }

    public static FlexTicketBundle FromPersistence(int id, int seasonId, TicketCategory category,
        string reference, DateTime createdAt, string? createdByName = null, string? createdByEmail = null) =>
        new(id, seasonId, category, reference, createdAt, Clean(createdByName), Clean(createdByEmail));

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
