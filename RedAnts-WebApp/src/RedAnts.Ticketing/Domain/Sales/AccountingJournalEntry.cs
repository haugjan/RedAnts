namespace RedAnts.Ticketing.Domain.Sales;

public sealed class AccountingJournalEntry
{
    public long Id { get; private set; }
    public long EntryNumber { get; private set; }
    public JournalEntryType EntryType { get; private set; }
    public int OrderId { get; private set; }
    public int? RefundId { get; private set; }
    public Money Amount { get; private set; }
    public decimal VatRate { get; private set; }
    public Money VatAmount { get; private set; }
    public string Currency => Amount.Currency;
    public string? Reference { get; private set; }
    public string? Description { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private AccountingJournalEntry(long id, long entryNumber, JournalEntryType entryType, int orderId, int? refundId,
        Money amount, decimal vatRate, Money vatAmount, string? reference, string? description,
        string? createdBy, DateTimeOffset occurredAt, DateTimeOffset createdAt)
    {
        Id = id;
        EntryNumber = entryNumber;
        EntryType = entryType;
        OrderId = orderId;
        RefundId = refundId;
        Amount = amount;
        VatRate = vatRate;
        VatAmount = vatAmount;
        Reference = reference;
        Description = description;
        CreatedBy = createdBy;
        OccurredAt = occurredAt;
        CreatedAt = createdAt;
    }

    public static AccountingJournalEntry FromPersistence(long id, long entryNumber, JournalEntryType entryType,
        int orderId, int? refundId, decimal amount, decimal vatRate, decimal vatAmount, string currency,
        string? reference, string? description, string? createdBy, DateTimeOffset occurredAt, DateTimeOffset createdAt) =>
        new(id, entryNumber, entryType, orderId, refundId, Money.Stored(amount, currency), vatRate,
            Money.Stored(vatAmount, currency), reference, description, createdBy, occurredAt, createdAt);
}
