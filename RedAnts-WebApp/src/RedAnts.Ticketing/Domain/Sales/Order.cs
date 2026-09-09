namespace RedAnts.Ticketing.Domain.Sales;

public static class RefundDenied
{
    public sealed record OrderUnknown() : CheckResult.Denied.Reason("Bestellung wurde nicht gefunden.");

    public sealed record NotPaid() : CheckResult.Denied.Reason("Nur bezahlte Bestellungen können zurückerstattet werden.");

    public sealed record NothingLeft() : CheckResult.Denied.Reason("Diese Bestellung ist vollständig zurückerstattet.");

    public sealed record AmountNotPositive() : CheckResult.Denied.Reason("Betrag muss grösser als 0 sein.");

    public sealed record AmountAboveRemaining(decimal Remaining)
        : CheckResult.Denied.Reason($"Der Betrag übersteigt den noch offenen Rest von CHF {Remaining:0.00}.");

    public sealed record NotPaidOnline() : CheckResult.Denied.Reason(
        "Diese Bestellung wurde nicht online über Payrexx bezahlt und kann nicht über Payrexx zurückerstattet werden.");
}

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
    public PaymentSource? PaymentSource { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public string? PayrexxGatewayId { get; private set; }
    public string? FulfillmentPayload { get; private set; }

    private Order(int id, string orderNumber, BillingAddress billingAddress, string currency,
        decimal subtotalNet, decimal vatRate, decimal vatAmount, decimal totalGross, string? sellerUid,
        PaymentMethod paymentMethod, PaymentSource? paymentSource, OrderStatus status, DateTimeOffset createdAt, DateTimeOffset? paidAt,
        string? payrexxGatewayId, string? fulfillmentPayload)
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
        PaymentSource = paymentSource;
        Status = status;
        CreatedAt = createdAt;
        PaidAt = paidAt;
        PayrexxGatewayId = payrexxGatewayId;
        FulfillmentPayload = fulfillmentPayload;
    }

    public static Order Create(string orderNumber, BillingAddress billingAddress, decimal totalGross,
        decimal vatRate, PaymentMethod paymentMethod, string? sellerUid, string currency = "CHF",
        PaymentSource? paymentSource = null, TimeProvider? time = null)
    {
        if (string.IsNullOrWhiteSpace(orderNumber)) throw new DomainException("Bestellnummer ist erforderlich.");
        if (totalGross < 0) throw new DomainException("Betrag darf nicht negativ sein.");

        var gross = decimal.Round(totalGross, 2);
        var vatAmount = vatRate <= 0 ? 0m : decimal.Round(gross - gross / (1 + vatRate), 2);
        var net = decimal.Round(gross - vatAmount, 2);

        return new Order(0, orderNumber.Trim(), billingAddress, currency, net, vatRate, vatAmount, gross,
            string.IsNullOrWhiteSpace(sellerUid) ? null : sellerUid.Trim(),
            paymentMethod, paymentSource, OrderStatus.Draft, SwissTime.TimestampOf(time), null, null, null);
    }

    public void SetPaymentSource(PaymentSource? paymentSource) => PaymentSource = paymentSource;

    public static Order FromPersistence(int id, string orderNumber, BillingAddress billingAddress, string currency,
        decimal subtotalNet, decimal vatRate, decimal vatAmount, decimal totalGross, string? sellerUid,
        PaymentMethod paymentMethod, OrderStatus status, DateTimeOffset createdAt, DateTimeOffset? paidAt,
        string? payrexxGatewayId = null, string? fulfillmentPayload = null, PaymentSource? paymentSource = null) =>
        new(id, orderNumber ?? "", billingAddress, string.IsNullOrWhiteSpace(currency) ? "CHF" : currency,
            subtotalNet, vatRate, vatAmount, totalGross, sellerUid, paymentMethod, paymentSource, status, createdAt, paidAt,
            payrexxGatewayId, fulfillmentPayload);

    public void SetFulfillmentPayload(string? payload) => FulfillmentPayload = payload;

    public void SetPayrexxGatewayId(string? gatewayId) => PayrexxGatewayId = gatewayId;

    public void MarkPaid(TimeProvider? time = null)
    {
        if (Status == OrderStatus.Cancelled) throw new DomainException("Stornierte Bestellung kann nicht bezahlt werden.");
        Status = OrderStatus.Paid;
        PaidAt ??= SwissTime.TimestampOf(time);
    }

    public void MarkUnpaid()
    {
        if (Status is OrderStatus.Cancelled or OrderStatus.Refunded)
            throw new DomainException("Stornierte oder erstattete Bestellung kann nicht auf unbezahlt gesetzt werden.");
        Status = OrderStatus.Draft;
        PaidAt = null;
    }

    public void Cancel() => Status = OrderStatus.Cancelled;
    public void Refund() => Status = OrderStatus.Refunded;

    public bool ChangeStatus(OrderStatus target, TimeProvider? time = null)
    {
        if (Status == target) return false;
        switch (target)
        {
            case OrderStatus.Paid: MarkPaid(time); break;
            case OrderStatus.Draft: MarkUnpaid(); break;
            case OrderStatus.Cancelled: Cancel(); break;
            case OrderStatus.Refunded: Refund(); break;
            default: throw new DomainException("Dieser Bezahlstatus kann nicht gesetzt werden.");
        }
        return true;
    }

    public bool DeactivatesTickets => Status is OrderStatus.Cancelled or OrderStatus.Refunded;

    public bool IsRefundable => Status is OrderStatus.Paid or OrderStatus.PartiallyRefunded;

    public CheckResult RefundBlocker(decimal remaining, decimal? amount = null, bool viaPayrexx = false, bool payrexxEnabled = true)
    {
        if (!IsRefundable) return CheckResult.Deny(new RefundDenied.NotPaid());
        if (remaining <= 0m) return CheckResult.Deny(new RefundDenied.NothingLeft());
        if (amount is { } value)
        {
            if (value <= 0m) return CheckResult.Deny(new RefundDenied.AmountNotPositive());
            if (value > remaining) return CheckResult.Deny(new RefundDenied.AmountAboveRemaining(remaining));
        }
        if (viaPayrexx && (!PaidThroughPayrexx || !payrexxEnabled)) return CheckResult.Deny(new RefundDenied.NotPaidOnline());
        return CheckResult.Allow();
    }

    public bool PaidThroughPayrexx => !string.IsNullOrWhiteSpace(PayrexxGatewayId);

    public void ApplyRefundTotal(decimal confirmedRefundTotal)
    {
        if (Status is OrderStatus.Cancelled or OrderStatus.Draft)
            throw new DomainException("Nur bezahlte Bestellungen können zurückerstattet werden.");
        if (confirmedRefundTotal <= 0) { Status = OrderStatus.Paid; return; }
        Status = confirmedRefundTotal >= TotalGross ? OrderStatus.Refunded : OrderStatus.PartiallyRefunded;
    }
}
