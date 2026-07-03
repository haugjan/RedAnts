namespace RedAnts.Domain.Ticketing.Sales;

/// <summary>A personal membership card (Mitgliederkarte), valid at every event of its season. Holder
/// name and birthday are optional (revDSG data minimization). Per-event admissions are tracked as
/// TicketEventVisits rows.</summary>
public sealed class MemberCard
{
    public int Id { get; private set; }
    public Guid Uuid { get; private set; }
    public int SeasonId { get; private set; }
    public TicketCategory Category { get; private set; }
    public decimal Price { get; private set; }
    public int? OrderId { get; private set; }
    public TicketStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public DateOnly? Birthday { get; private set; }

    public string HolderName => $"{FirstName} {LastName}".Trim();

    private MemberCard(int id, Guid uuid, int seasonId, TicketCategory category, decimal price,
        int? orderId, TicketStatus status, DateTime createdAt, string? firstName, string? lastName, DateOnly? birthday)
    {
        Id = id;
        Uuid = uuid;
        SeasonId = seasonId;
        Category = category;
        Price = price;
        OrderId = orderId;
        Status = status;
        CreatedAt = createdAt;
        FirstName = firstName;
        LastName = lastName;
        Birthday = birthday;
    }

    public static MemberCard Create(int seasonId, TicketCategory category, decimal price, int? orderId,
        string? firstName, string? lastName, DateOnly? birthday)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
        if (price < 0) throw new DomainException("Preis darf nicht negativ sein.");
        return new MemberCard(0, Guid.NewGuid(), seasonId, category, decimal.Round(price, 2),
            orderId, TicketStatus.Valid, DateTime.UtcNow, Clean(firstName), Clean(lastName), birthday);
    }

    public static MemberCard FromPersistence(int id, Guid uuid, int seasonId, TicketCategory category,
        decimal price, int? orderId, TicketStatus status, DateTime createdAt, string? firstName,
        string? lastName, DateOnly? birthday) =>
        new(id, uuid, seasonId, category, price, orderId, status, createdAt, firstName, lastName, birthday);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
