namespace RedAnts.Domain.Ticketing.Sales;

/// <summary>A season pass (Saisonkarte): valid at every event of its season. Per-event admissions are
/// tracked in <see cref="EventVisit"/> rows, so a holder can enter each event independently (in/out).</summary>
public sealed class SeasonPass
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
    /// <summary>Scanner/staff that last redeemed the pass (latest visit); null until first use.</summary>
    public string? RedeemedBy { get; private set; }

    private SeasonPass(int id, Guid uuid, int seasonId, string categoryCode, string categoryName, decimal price,
        int? orderId, TicketStatus status, DateTime createdAt, string? redeemedBy)
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
        RedeemedBy = redeemedBy;
    }

    public static SeasonPass Create(int seasonId, string categoryCode, string categoryName, decimal price, int? orderId)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
        if (price < 0) throw new DomainException("Preis darf nicht negativ sein.");
        return new SeasonPass(0, Guid.NewGuid(), seasonId, categoryCode ?? "", categoryName ?? "",
            decimal.Round(price, 2), orderId, TicketStatus.Valid, DateTime.UtcNow, null);
    }

    public static SeasonPass FromPersistence(int id, Guid uuid, int seasonId, string categoryCode, string categoryName,
        decimal price, int? orderId, TicketStatus status, DateTime createdAt, string? redeemedBy) =>
        new(id, uuid, seasonId, categoryCode ?? "", categoryName ?? "", price, orderId, status, createdAt, redeemedBy);

    public void RecordRedemption(string? scannedBy) => RedeemedBy = scannedBy;
}
