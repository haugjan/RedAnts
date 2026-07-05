namespace RedAnts.Domain.Ticketing.Sales;

public sealed class MemberCard
{
    public int Id { get; private set; }
    public Guid Uuid { get; private set; }
    public int SeasonId { get; private set; }
    public MemberCategory Category { get; private set; }
    public int? OrderId { get; private set; }
    public TicketStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public DateOnly? Birthday { get; private set; }
    public string? Reference { get; private set; }
    public string? CreatedByName { get; private set; }
    public string? CreatedByEmail { get; private set; }

    public string HolderName => $"{FirstName} {LastName}".Trim();

    private MemberCard(int id, Guid uuid, int seasonId, MemberCategory category,
        int? orderId, TicketStatus status, DateTime createdAt, string? firstName, string? lastName,
        DateOnly? birthday, string? reference, string? createdByName, string? createdByEmail)
    {
        Id = id;
        Uuid = uuid;
        SeasonId = seasonId;
        Category = category;
        OrderId = orderId;
        Status = status;
        CreatedAt = createdAt;
        FirstName = firstName;
        LastName = lastName;
        Birthday = birthday;
        Reference = reference;
        CreatedByName = createdByName;
        CreatedByEmail = createdByEmail;
    }

    public static MemberCard Create(int seasonId, MemberCategory category, string? firstName, string? lastName,
        DateOnly? birthday, string? reference = null, int? orderId = null, string? createdByName = null,
        string? createdByEmail = null)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
        return new MemberCard(0, Guid.NewGuid(), seasonId, category,
            orderId, TicketStatus.Valid, DateTime.UtcNow, Clean(firstName), Clean(lastName), birthday,
            Clean(reference), Clean(createdByName), Clean(createdByEmail));
    }

    public static MemberCard FromPersistence(int id, Guid uuid, int seasonId, MemberCategory category,
        int? orderId, TicketStatus status, DateTime createdAt, string? firstName, string? lastName,
        DateOnly? birthday, string? reference, string? createdByName = null, string? createdByEmail = null) =>
        new(id, uuid, seasonId, category, orderId, status, createdAt, firstName, lastName, birthday,
            Clean(reference), Clean(createdByName), Clean(createdByEmail));

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
