using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Cart;

public sealed class CheckoutForm
{
    public BuyerType Type { get; set; } = BuyerType.Private;
    public string? Company { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Street { get; set; } = "";
    public string? AddressLine2 { get; set; }
    public string PostalCode { get; set; } = "";
    public string City { get; set; } = "";
    public string Country { get; set; } = "Schweiz";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
}

public sealed class CheckoutAddressView
{
    public CheckoutForm Form { get; init; } = new();
    public Cart Cart { get; init; } = new();
    public string? Error { get; init; }
}

public sealed record PaymentOption(PaymentMethod Method, string Label, string Hint);

public sealed class CheckoutPaymentView
{
    public Cart Cart { get; init; } = new();
    public CheckoutForm Form { get; init; } = new();
    public IReadOnlyList<PaymentOption> Methods { get; init; } = [];
}

public sealed record ConfirmationTicket(Guid Uuid, string EventName, string CategoryName);

public sealed class CheckoutConfirmationView
{
    public string OrderNumber { get; init; } = "";
    public string Email { get; init; } = "";
    public decimal Total { get; init; }
    public string PaymentLabel { get; init; } = "";
    public IReadOnlyList<ConfirmationTicket> Tickets { get; init; } = [];
}
