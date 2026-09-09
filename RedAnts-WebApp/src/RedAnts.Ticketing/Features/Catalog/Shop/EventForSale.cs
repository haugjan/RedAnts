using RedAnts.Ticketing.Features.Checkout;

namespace RedAnts.Ticketing.Features.Catalog.Shop;

public sealed record VenueForSale(int Id, string Name, string? GoogleGeoId, string? ImageUrl, string? Arrival, string? Address);

public sealed record EventForSale(
    int Id,
    string Name,
    string? Text,
    DateOnly Date,
    TimeOnly StartTime,
    bool TimeUnknown,
    string? ImageUrl,
    string? HomeTeamLogoUrl,
    string? AwayTeamLogoUrl,
    VenueForSale? Venue,
    string? Url,
    IReadOnlyList<AvailableTicketCategory> Categories,
    bool ConversionOnly,
    bool OffersConversion);
