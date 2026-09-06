namespace RedAnts.Domain.Ticketing.Sales;

public sealed class OrderRefund
{
    public int Id { get; private set; }
    public string RefundNumber { get; private set; }
    public int OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public decimal VatRate { get; private set; }
    public decimal VatAmount { get; private set; }
    public string Currency { get; private set; }
    public RefundMethod Method { get; private set; }
    public RefundStatus Status { get; private set; }
    public string? PayrexxRefundId { get; private set; }
    public string? Reference { get; private set; }
    public string? Reason { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private OrderRefund(int id, string refundNumber, int orderId, decimal amount, decimal vatRate, decimal vatAmount,
        string currency, RefundMethod method, RefundStatus status, string? payrexxRefundId, string? reference,
        string? reason, string? createdBy, DateTime createdAt)
    {
        Id = id;
        RefundNumber = refundNumber;
        OrderId = orderId;
        Amount = amount;
        VatRate = vatRate;
        VatAmount = vatAmount;
        Currency = currency;
        Method = method;
        Status = status;
        PayrexxRefundId = payrexxRefundId;
        Reference = reference;
        Reason = reason;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }

    public static OrderRefund Create(string refundNumber, int orderId, decimal amount, decimal vatRate,
        RefundMethod method, RefundStatus status, string? reference, string? reason, string? createdBy,
        string currency = "CHF")
    {
        if (amount <= 0) throw new DomainException("Rückzahlungsbetrag muss grösser als 0 sein.");
        var value = decimal.Round(amount, 2);
        var vat = vatRate <= 0 ? 0m : decimal.Round(value - value / (1 + vatRate), 2);
        return new OrderRefund(0, refundNumber.Trim(), orderId, value, vatRate, vat, currency, method, status,
            null, Clean(reference), Clean(reason), Clean(createdBy), DateTime.UtcNow);
    }

    public static OrderRefund FromPersistence(int id, string refundNumber, int orderId, decimal amount, decimal vatRate,
        decimal vatAmount, string currency, RefundMethod method, RefundStatus status, string? payrexxRefundId,
        string? reference, string? reason, string? createdBy, DateTime createdAt) =>
        new(id, refundNumber ?? "", orderId, amount, vatRate, vatAmount,
            string.IsNullOrWhiteSpace(currency) ? "CHF" : currency, method, status, payrexxRefundId,
            reference, reason, createdBy, createdAt);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
