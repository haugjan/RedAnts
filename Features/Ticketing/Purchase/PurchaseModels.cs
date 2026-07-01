using RedAnts.Domain.Ticketing;

namespace RedAnts.Features.Ticketing.Purchase;

/// <summary>Billing details submitted by the buyer.</summary>
public sealed record BillingInput(
    string FirstName,
    string LastName,
    string Street,
    string? AddressLine2,
    string PostalCode,
    string City,
    string? Country,
    string Email,
    string? Phone)
{
    public BillingAddress ToBillingAddress() =>
        BillingAddress.Create(FirstName, LastName, Street, AddressLine2, PostalCode, City, Country, Email, Phone);
}

public sealed record StartSinglePurchaseCommand(int EventId, PriceCategory Category, BillingInput Billing);

public sealed record StartSeasonPurchaseCommand(int SeasonId, SeasonTicketCategory Category, AgeGroup AgeGroup, BillingInput Billing);

/// <summary>Result of starting a purchase: the secret ticket reference and the URL to redirect the buyer to.</summary>
public sealed record PurchaseStarted(Guid TicketRef, string RedirectUrl);
