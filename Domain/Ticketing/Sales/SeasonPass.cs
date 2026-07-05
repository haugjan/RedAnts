namespace RedAnts.Domain.Ticketing.Sales;

public sealed class SeasonPass
{
    public int Id { get; private set; }
    public Guid Uuid { get; private set; }
    public int SeasonId { get; private set; }
    public TicketCategory Category { get; private set; }
    public decimal Price { get; private set; }
    public int? OrderId { get; private set; }
    public TicketStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private SeasonPass(int id, Guid uuid, int seasonId, TicketCategory category, decimal price,
        int? orderId, TicketStatus status, DateTime createdAt)
    {
        Id = id;
        Uuid = uuid;
        SeasonId = seasonId;
        Category = category;
        Price = price;
        OrderId = orderId;
        Status = status;
        CreatedAt = createdAt;
    }

    public static SeasonPass Create(int seasonId, TicketCategory category, decimal price, int? orderId)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
        if (price < 0) throw new DomainException("Preis darf nicht negativ sein.");
        return new SeasonPass(0, Guid.NewGuid(), seasonId, category, decimal.Round(price, 2),
            orderId, TicketStatus.Valid, DateTime.UtcNow);
    }

    public static SeasonPass FromPersistence(int id, Guid uuid, int seasonId, TicketCategory category,
        decimal price, int? orderId, TicketStatus status, DateTime createdAt) =>
        new(id, uuid, seasonId, category, price, orderId, status, createdAt);

    public void Edit(TicketCategory category, decimal price, TicketStatus status)
    {
        if (price < 0) throw new DomainException("Preis darf nicht negativ sein.");
        Category = category;
        Price = decimal.Round(price, 2);
        Status = status;
    }
}
