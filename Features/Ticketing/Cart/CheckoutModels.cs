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
    public bool AcceptNewsletter { get; set; }
}

public sealed class CheckoutAddressView
{
    public CheckoutForm Form { get; init; } = new();
    public Cart Cart { get; init; } = new();
    public bool PayrexxEnabled { get; init; }
    public string? TurnstileSiteKey { get; init; }
    public string? Error { get; init; }
}

public sealed class CheckoutPaymentView
{
    public Cart Cart { get; init; } = new();
    public CheckoutForm Form { get; init; } = new();
    public bool PayrexxEnabled { get; init; }
    public string? TurnstileSiteKey { get; init; }
    public string? Error { get; init; }
}

public sealed class CheckoutExpressView
{
    public Cart Cart { get; init; } = new();
    public bool PayrexxEnabled { get; init; }
    public string? TurnstileSiteKey { get; init; }
    public string? Error { get; init; }
    public string Email { get; init; } = "";
    public string Name { get; init; } = "";
}

public sealed class CheckoutProcessingView
{
    public int OrderId { get; init; }
    public string OrderNumber { get; init; } = "";
    public string Email { get; init; } = "";
    public bool AlreadyPaid { get; init; }
}

public sealed record FulfillmentItem(
    int Kind, int EventId, int SeasonId, int TierId, decimal UnitPrice, int Quantity, string EventName, string CategoryName);

public sealed record FulfillmentAddOn(
    int Id, int SeasonId, string EventName, int TierId, string CategoryName, string Label, decimal Price, int Quantity);

public sealed record FulfillmentSnapshot(
    List<FulfillmentItem> Items,
    List<FulfillmentAddOn> AddOns,
    bool SubscribeNewsletter,
    string NewsletterSource);

public sealed record ConfirmationTicket(Guid Uuid, string EventName, string CategoryName, string Token, int Type = 0, string? DateText = null);

public sealed class CheckoutConfirmationView
{
    public string OrderNumber { get; init; } = "";
    public string Email { get; init; } = "";
    public decimal Total { get; init; }
    public string PaymentLabel { get; init; } = "";
    public IReadOnlyList<ConfirmationTicket> Tickets { get; init; } = [];
    public IReadOnlyList<string> AddOnInfoTexts { get; init; } = [];
}
