namespace RedAnts.Ticketing.Domain.Sales;

public sealed class OrderRefund
{
    public int Id { get; private set; }
    public string RefundNumber { get; private set; }
    public int OrderId { get; private set; }
    public Money Amount { get; private set; }
    public decimal VatRate { get; private set; }
    public Money VatAmount { get; private set; }
    public string Currency => Amount.Currency;
    public RefundMethod Method { get; private set; }
    public RefundStatus Status { get; private set; }
    public string? PayrexxRefundId { get; private set; }
    public string? Reference { get; private set; }
    public string? Reason { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private OrderRefund(int id, string refundNumber, int orderId, Money amount, decimal vatRate, Money vatAmount,
        RefundMethod method, RefundStatus status, string? payrexxRefundId, string? reference,
        string? reason, string? createdBy, DateTimeOffset createdAt)
    {
        Id = id;
        RefundNumber = refundNumber;
        OrderId = orderId;
        Amount = amount;
        VatRate = vatRate;
        VatAmount = vatAmount;
        Method = method;
        Status = status;
        PayrexxRefundId = payrexxRefundId;
        Reference = reference;
        Reason = reason;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }

    public static OrderRefund Create(string refundNumber, int orderId, Money amount, decimal vatRate,
        RefundMethod method, RefundStatus status, string? reference, string? reason, string? createdBy,
        TimeProvider? time = null)
    {
        var value = Money.Of(amount.Amount, amount.Currency, "amount", "Rückzahlungsbetrag");
        if (!value.IsPositive) throw new DomainException("Rückzahlungsbetrag muss grösser als 0 sein.");
        var vat = vatRate <= 0
            ? Money.Of(0m, value.Currency)
            : Money.Of(value.Amount - value.Amount / (1 + vatRate), value.Currency);
        return new OrderRefund(0, refundNumber.Trim(), orderId, value, vatRate, vat, method, status,
            null, Clean(reference), Clean(reason), Clean(createdBy), SwissTime.TimestampOf(time));
    }

    public static OrderRefund FromPersistence(int id, string refundNumber, int orderId, decimal amount, decimal vatRate,
        decimal vatAmount, string currency, RefundMethod method, RefundStatus status, string? payrexxRefundId,
        string? reference, string? reason, string? createdBy, DateTimeOffset createdAt) =>
        new(id, refundNumber ?? "", orderId, Money.Stored(amount, currency), vatRate, Money.Stored(vatAmount, currency),
            method, status, payrexxRefundId, reference, reason, createdBy, createdAt);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
