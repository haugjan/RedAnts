namespace RedAnts.Domain.Ticketing.Sales;

public sealed class AccountingJournalEntry
{
    public long Id { get; private set; }
    public long EntryNumber { get; private set; }
    public JournalEntryType EntryType { get; private set; }
    public int OrderId { get; private set; }
    public int? RefundId { get; private set; }
    public decimal Amount { get; private set; }
    public decimal VatRate { get; private set; }
    public decimal VatAmount { get; private set; }
    public string Currency { get; private set; }
    public string? Reference { get; private set; }
    public string? Description { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private AccountingJournalEntry(long id, long entryNumber, JournalEntryType entryType, int orderId, int? refundId,
        decimal amount, decimal vatRate, decimal vatAmount, string currency, string? reference, string? description,
        string? createdBy, DateTime occurredAt, DateTime createdAt)
    {
        Id = id;
        EntryNumber = entryNumber;
        EntryType = entryType;
        OrderId = orderId;
        RefundId = refundId;
        Amount = amount;
        VatRate = vatRate;
        VatAmount = vatAmount;
        Currency = currency;
        Reference = reference;
        Description = description;
        CreatedBy = createdBy;
        OccurredAt = occurredAt;
        CreatedAt = createdAt;
    }

    public static AccountingJournalEntry FromPersistence(long id, long entryNumber, JournalEntryType entryType,
        int orderId, int? refundId, decimal amount, decimal vatRate, decimal vatAmount, string currency,
        string? reference, string? description, string? createdBy, DateTime occurredAt, DateTime createdAt) =>
        new(id, entryNumber, entryType, orderId, refundId, amount, vatRate, vatAmount,
            string.IsNullOrWhiteSpace(currency) ? "CHF" : currency, reference, description, createdBy, occurredAt, createdAt);
}
