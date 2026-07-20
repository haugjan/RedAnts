namespace RedAnts.Domain.Ticketing.Sales;

public sealed class SeasonPass
{
    public int Id { get; private set; }
    public Guid Uuid { get; private set; }
    public int SeasonId { get; private set; }
    public TicketCategory Category { get; private set; }
    public int? TierId { get; private set; }
    public decimal Price { get; private set; }
    public int? OrderId { get; private set; }
    public TicketStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Buyer? Buyer { get; private set; }
    public string? CreatedByName { get; private set; }
    public string? CreatedByEmail { get; private set; }
    public string? Reference { get; private set; }

    private SeasonPass(int id, Guid uuid, int seasonId, TicketCategory category, int? tierId, decimal price,
        int? orderId, TicketStatus status, DateTime createdAt, Buyer? buyer,
        string? createdByName, string? createdByEmail, string? reference)
    {
        Id = id;
        Uuid = uuid;
        SeasonId = seasonId;
        Category = category;
        TierId = tierId;
        Price = price;
        OrderId = orderId;
        Status = status;
        CreatedAt = createdAt;
        Buyer = buyer;
        CreatedByName = createdByName;
        CreatedByEmail = createdByEmail;
        Reference = reference;
    }

    public static SeasonPass Create(int seasonId, TicketCategory category, decimal price, int? orderId,
        Buyer? buyer = null, string? createdByName = null, string? createdByEmail = null, string? reference = null,
        int? tierId = null)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
        if (price < 0) throw new DomainException("Preis darf nicht negativ sein.");
        return new SeasonPass(0, Guid.NewGuid(), seasonId, category, tierId, decimal.Round(price, 2),
            orderId, TicketStatus.Valid, DateTime.UtcNow, buyer, Clean(createdByName), Clean(createdByEmail), Clean(reference));
    }

    public static SeasonPass FromPersistence(int id, Guid uuid, int seasonId, TicketCategory category,
        decimal price, int? orderId, TicketStatus status, DateTime createdAt,
        Buyer? buyer = null, string? createdByName = null, string? createdByEmail = null, string? reference = null,
        int? tierId = null) =>
        new(id, uuid, seasonId, category, tierId, price, orderId, status, createdAt, buyer,
            Clean(createdByName), Clean(createdByEmail), Clean(reference));

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public void Edit(TicketCategory category, decimal price, TicketStatus status, int? tierId = null)
    {
        if (price < 0) throw new DomainException("Preis darf nicht negativ sein.");
        Category = category;
        if (tierId is not null) TierId = tierId;
        Price = decimal.Round(price, 2);
        Status = status;
    }

    public void SetBuyer(Buyer buyer) => Buyer = buyer;
}
