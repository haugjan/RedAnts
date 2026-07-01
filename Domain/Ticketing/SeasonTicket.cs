namespace RedAnts.Domain.Ticketing;

/// <summary>A season ticket (Saisonkarte) valid for a whole season.
/// Block4 grants 4 admissions (tracked via <see cref="RemainingAdmissions"/>); Members/Extern are unlimited.</summary>
public sealed class SeasonTicket
{
    public int Id { get; private set; }
    public int SeasonId { get; private set; }
    public SeasonTicketCategory Category { get; private set; }
    public AgeGroup AgeGroup { get; private set; }
    public decimal Price { get; private set; }
    public DateTime PurchasedAt { get; private set; }
    public Guid SeasonTicketId { get; private set; }
    public BillingAddress BillingAddress { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public PayStatus PayStatus { get; private set; }

    /// <summary>Remaining admissions for limited categories (Block4 = 4). Null = unlimited.</summary>
    public int? RemainingAdmissions { get; private set; }
    public DateTime? CheckedInAt { get; private set; }

    public bool IsInside => CheckedInAt is not null;

    private SeasonTicket(int id, int seasonId, SeasonTicketCategory category, AgeGroup ageGroup, decimal price,
        DateTime purchasedAt, Guid seasonTicketId, BillingAddress billingAddress, PaymentMethod paymentMethod,
        PayStatus payStatus, int? remainingAdmissions, DateTime? checkedInAt)
    {
        Id = id;
        SeasonId = seasonId;
        Category = category;
        AgeGroup = ageGroup;
        Price = price;
        PurchasedAt = purchasedAt;
        SeasonTicketId = seasonTicketId;
        BillingAddress = billingAddress;
        PaymentMethod = paymentMethod;
        PayStatus = payStatus;
        RemainingAdmissions = remainingAdmissions;
        CheckedInAt = checkedInAt;
    }

    public static SeasonTicket CreatePending(int seasonId, SeasonTicketCategory category, AgeGroup ageGroup,
        decimal price, BillingAddress billingAddress, PaymentMethod paymentMethod)
    {
        if (seasonId <= 0) throw new DomainException("Eine Saison muss zugewiesen sein.");
        if (price < 0) throw new DomainException("Preis darf nicht negativ sein.");
        var remaining = category == SeasonTicketCategory.Block4 ? 4 : (int?)null;
        return new SeasonTicket(0, seasonId, category, ageGroup, decimal.Round(price, 2), DateTime.UtcNow,
            Guid.NewGuid(), billingAddress, paymentMethod, PayStatus.Pending, remaining, null);
    }

    public static SeasonTicket FromPersistence(int id, int seasonId, SeasonTicketCategory category, AgeGroup ageGroup,
        decimal price, DateTime purchasedAt, Guid seasonTicketId, BillingAddress billingAddress,
        PaymentMethod paymentMethod, PayStatus payStatus, int? remainingAdmissions, DateTime? checkedInAt) =>
        new(id, seasonId, category, ageGroup, price, purchasedAt, seasonTicketId, billingAddress,
            paymentMethod, payStatus, remainingAdmissions, checkedInAt);

    public void MarkPaid() => PayStatus = PayStatus.Paid;
    public void MarkFailed() => PayStatus = PayStatus.Failed;
    public void MarkRefunded() => PayStatus = PayStatus.Refunded;

    /// <summary>Scan-in. Consumes one admission for limited (Block4) tickets.</summary>
    public void CheckIn()
    {
        if (PayStatus != PayStatus.Paid) throw new DomainException("Saisonkarte ist nicht bezahlt.");
        if (IsInside) throw new DomainException("Saisonkarte ist bereits eingecheckt.");
        if (RemainingAdmissions is <= 0) throw new DomainException("Keine Eintritte mehr verfügbar.");
        if (RemainingAdmissions is int remaining) RemainingAdmissions = remaining - 1;
        CheckedInAt = DateTime.UtcNow;
    }

    /// <summary>Scan-out (Auscannen). Refunds the consumed admission for limited tickets so re-entry is possible.</summary>
    public void CheckOut()
    {
        if (!IsInside) throw new DomainException("Saisonkarte ist nicht eingecheckt.");
        if (RemainingAdmissions is int remaining) RemainingAdmissions = remaining + 1;
        CheckedInAt = null;
    }
}
