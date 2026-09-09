namespace RedAnts.Ticketing.Features.Catalog.Shop;

public sealed record VenueDetail(int Id, string Name, string? GoogleGeoId, string? ImageUrl, string? Description, string? Address);

public static class GetVenue
{
    public sealed record Query(int VenueId);

    public sealed class Handler(IVenueReader venues)
    {
        public async Task<VenueDetail?> HandleAsync(Query query) =>
            await venues.FindByIdAsync(query.VenueId) is { } venue
                ? new VenueDetail(venue.Id, venue.Name, venue.GoogleGeoId, venue.ImageUrl, venue.Description, venue.Address)
                : null;
    }
}
