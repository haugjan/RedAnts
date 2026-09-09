using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Features.Catalog.Shop;
using RedAnts.Ticketing.Features.Checkout;

namespace RedAnts.Ticketing.Features.Catalog.Infrastructure;

internal static class SaleOffers
{
    public static bool Buyable(IReadOnlyList<AvailableTicketCategory> categories) => categories.Any(c => c.Available);

    public static bool SoldOut(IReadOnlyList<AvailableTicketCategory> categories) =>
        !categories.Any(c => c.Available) && categories.Any(c => c.Remaining == 0);

    public static UpcomingEvent ToUpcoming(Event e, string? venueName, string? url, IReadOnlyList<AvailableTicketCategory> categories) =>
        new(e.Id, e.Name, e.Date, e.StartTime, e.TimeUnknown, venueName, e.ImageUrl, e.HomeTeamLogoUrl, e.AwayTeamLogoUrl, url,
            Buyable(categories), SoldOut(categories));
}

internal sealed class VenueNames(IVenueReader venues)
{
    private readonly Dictionary<int, string?> _names = new();

    public async Task<string?> ForAsync(int venueId)
    {
        if (venueId <= 0) return null;
        if (!_names.TryGetValue(venueId, out var name))
            _names[venueId] = name = (await venues.FindByIdAsync(venueId))?.Name;
        return name;
    }
}
