namespace RedAnts.Domain.Ticketing.Sales;

/// <summary>
/// A Bestellung (order) = the immutable financial record for a sale (Beleg/Rechnung).
/// Holds the billing address and payment/VAT data; tickets reference it optionally.
/// Swiss compliance: kept ≥10 years (OR 957–958f), never hard-deleted; a cancellation or refund
/// is a <see cref="OrderStatus"/> change. VAT-ready (sports entry fees are usually exempt,
/// Art. 21 Abs. 2 Ziff. 15 MWSTG); default treatment is 0 % / exempt via configuration.
/// </summary>
public sealed class Order
{
    public int Id { get; private set; }
    /// <summary>Sequential, unique, immutable invoice/order number (e.g. 2026-000123).</summary>
    public string OrderNumber { get; private set; }
    public BillingAddress BillingAddress { get; private set; }
    public string Currency { get; private set; }
    public decimal SubtotalNet { get; private set; }
    public decimal VatRate { get; private set; }
    public decimal VatAmount { get; private set; }
    public decimal TotalGross { get; private set; }
    /// <summary>Seller UID/MWST number printed on the receipt (from config; null when not VAT-liable).</summary>
    public string? SellerUid { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? PaidAt { get; private set; }

    private Order(int id, string orderNumber, BillingAddress billingAddress, string currency,
        decimal subtotalNet, decimal vatRate, decimal vatAmount, decimal totalGross, string? sellerUid,
        PaymentMethod paymentMethod, OrderStatus status, DateTime createdAt, DateTime? paidAt)
    {
        Id = id;
        OrderNumber = orderNumber;
        BillingAddress = billingAddress;
        Currency = currency;
        SubtotalNet = subtotalNet;
        VatRate = vatRate;
        VatAmount = vatAmount;
        TotalGross = totalGross;
        SellerUid = sellerUid;
        PaymentMethod = paymentMethod;
        Status = status;
        CreatedAt = createdAt;
        PaidAt = paidAt;
    }

    public static Order Create(string orderNumber, BillingAddress billingAddress, decimal totalGross,
        decimal vatRate, PaymentMethod paymentMethod, string? sellerUid, string currency = "CHF")
    {
        if (string.IsNullOrWhiteSpace(orderNumber)) throw new DomainException("Bestellnummer ist erforderlich.");
        if (totalGross < 0) throw new DomainException("Betrag darf nicht negativ sein.");

        var gross = decimal.Round(totalGross, 2);
        // VAT-inclusive gross: net = gross / (1 + rate); exempt (rate 0) -> vat 0, net = gross.
        var vatAmount = vatRate <= 0 ? 0m : decimal.Round(gross - gross / (1 + vatRate), 2);
        var net = decimal.Round(gross - vatAmount, 2);

        return new Order(0, orderNumber.Trim(), billingAddress, currency, net, vatRate, vatAmount, gross,
            string.IsNullOrWhiteSpace(sellerUid) ? null : sellerUid.Trim(),
            paymentMethod, OrderStatus.Draft, DateTime.UtcNow, null);
    }

    public static Order FromPersistence(int id, string orderNumber, BillingAddress billingAddress, string currency,
        decimal subtotalNet, decimal vatRate, decimal vatAmount, decimal totalGross, string? sellerUid,
        PaymentMethod paymentMethod, OrderStatus status, DateTime createdAt, DateTime? paidAt) =>
        new(id, orderNumber ?? "", billingAddress, string.IsNullOrWhiteSpace(currency) ? "CHF" : currency,
            subtotalNet, vatRate, vatAmount, totalGross, sellerUid, paymentMethod, status, createdAt, paidAt);

    public void MarkPaid()
    {
        if (Status == OrderStatus.Cancelled) throw new DomainException("Stornierte Bestellung kann nicht bezahlt werden.");
        Status = OrderStatus.Paid;
        PaidAt ??= DateTime.UtcNow;
    }

    public void Cancel() => Status = OrderStatus.Cancelled;
    public void Refund() => Status = OrderStatus.Refunded;
}
