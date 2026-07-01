namespace RedAnts.Domain.Ticketing;

/// <summary>A single ticket bought for one specific event.</summary>
public sealed class SingleTicket
{
    public int Id { get; private set; }
    public int EventId { get; private set; }
    public PriceCategory PriceCategory { get; private set; }
    public decimal Price { get; private set; }
    public DateTime PurchasedAt { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public DateTime? CheckedInAt { get; private set; }
    public Guid TicketId { get; private set; }
    public BillingAddress BillingAddress { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public PayStatus PayStatus { get; private set; }

    /// <summary>True while the holder is currently checked in (inside the building).</summary>
    public bool IsInside => CheckedInAt is not null;

    private SingleTicket(int id, int eventId, PriceCategory priceCategory, decimal price, DateTime purchasedAt,
        DateTime? usedAt, DateTime? checkedInAt, Guid ticketId, BillingAddress billingAddress,
        PaymentMethod paymentMethod, PayStatus payStatus)
    {
        Id = id;
        EventId = eventId;
        PriceCategory = priceCategory;
        Price = price;
        PurchasedAt = purchasedAt;
        UsedAt = usedAt;
        CheckedInAt = checkedInAt;
        TicketId = ticketId;
        BillingAddress = billingAddress;
        PaymentMethod = paymentMethod;
        PayStatus = payStatus;
    }

    public static SingleTicket CreatePending(int eventId, PriceCategory priceCategory, decimal price,
        BillingAddress billingAddress, PaymentMethod paymentMethod)
    {
        if (eventId <= 0) throw new DomainException("Ein Anlass muss zugewiesen sein.");
        if (price < 0) throw new DomainException("Preis darf nicht negativ sein.");
        return new SingleTicket(0, eventId, priceCategory, decimal.Round(price, 2), DateTime.UtcNow,
            null, null, Guid.NewGuid(), billingAddress, paymentMethod, PayStatus.Pending);
    }

    public static SingleTicket FromPersistence(int id, int eventId, PriceCategory priceCategory, decimal price,
        DateTime purchasedAt, DateTime? usedAt, DateTime? checkedInAt, Guid ticketId, BillingAddress billingAddress,
        PaymentMethod paymentMethod, PayStatus payStatus) =>
        new(id, eventId, priceCategory, price, purchasedAt, usedAt, checkedInAt, ticketId,
            billingAddress, paymentMethod, payStatus);

    public void MarkPaid() => PayStatus = PayStatus.Paid;
    public void MarkFailed() => PayStatus = PayStatus.Failed;
    public void MarkRefunded() => PayStatus = PayStatus.Refunded;

    /// <summary>Scan-in. A ticket can only be checked in once at a time.</summary>
    public void CheckIn()
    {
        if (PayStatus != PayStatus.Paid) throw new DomainException("Ticket ist nicht bezahlt.");
        if (IsInside) throw new DomainException("Ticket ist bereits eingecheckt.");
        CheckedInAt = DateTime.UtcNow;
        UsedAt ??= CheckedInAt;
    }

    /// <summary>Scan-out (Auscannen). The ticket becomes available again for re-entry.</summary>
    public void CheckOut()
    {
        if (!IsInside) throw new DomainException("Ticket ist nicht eingecheckt.");
        CheckedInAt = null;
    }
}
