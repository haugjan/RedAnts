namespace RedAnts.Domain.Ticketing.Sales;

public sealed class Order
{
    public int Id { get; private set; }
    public string OrderNumber { get; private set; }
    public BillingAddress BillingAddress { get; private set; }
    public string Currency { get; private set; }
    public decimal SubtotalNet { get; private set; }
    public decimal VatRate { get; private set; }
    public decimal VatAmount { get; private set; }
    public decimal TotalGross { get; private set; }
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
