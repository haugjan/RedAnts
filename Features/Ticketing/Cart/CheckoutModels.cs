using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Cart;

/// <summary>Billing details captured in step 1 of the checkout. Bound from the address form and kept in
/// the session between the two checkout steps; validated by turning it into a <c>BillingAddress</c>.</summary>
public sealed class CheckoutForm
{
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

/// <summary>View model for the address step (step 1).</summary>
public sealed class CheckoutAddressView
{
    public CheckoutForm Form { get; init; } = new();
    public Cart Cart { get; init; } = new();
    public string? Error { get; init; }
}

/// <summary>One selectable payment method on the payment step.</summary>
public sealed record PaymentOption(PaymentMethod Method, string Label, string Hint);

/// <summary>View model for the payment step (step 2).</summary>
public sealed class CheckoutPaymentView
{
    public Cart Cart { get; init; } = new();
    public CheckoutForm Form { get; init; } = new();
    public IReadOnlyList<PaymentOption> Methods { get; init; } = [];
}

/// <summary>One issued ticket shown on the confirmation page (links to its online ticket).</summary>
public sealed record ConfirmationTicket(Guid Uuid, string EventName, string CategoryName);

/// <summary>View model + session payload for the confirmation page after a (pseudo) successful sale.</summary>
public sealed class CheckoutConfirmationView
{
    public string OrderNumber { get; init; } = "";
    public string Email { get; init; } = "";
    public decimal Total { get; init; }
    public string PaymentLabel { get; init; } = "";
    public IReadOnlyList<ConfirmationTicket> Tickets { get; init; } = [];
}
