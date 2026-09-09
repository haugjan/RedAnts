using RedAnts.Ticketing.Features.Catalog.Shop;
using RedAnts.Ticketing.Features.Checkout;

namespace RedAnts.Ticketing.Features.Catalog.Infrastructure;

public sealed class NextEventReader(IEventReader events, IVenueReader venues, IEventPricing pricing, IContentUrls urls) : INextEventReader
{
    public async Task<NextEvent?> FindAsync()
    {
        var today = SwissTime.Today;
        var upcoming = await events.GetPublicOpenAsync();
        var target = upcoming.FirstOrDefault(e => e.Date == today) ?? upcoming.FirstOrDefault();
        if (target is null) return null;

        var categories = await pricing.GetAvailableAsync(target.Id);
        var venue = target.VenueId > 0 ? await venues.FindByIdAsync(target.VenueId) : null;
        return new NextEvent(target.Id, target.Name, target.Date, target.StartTime, target.TimeUnknown, target.ImageUrl,
            target.HomeTeamLogoUrl, target.AwayTeamLogoUrl, venue?.Name, urls.GetUrl(target.Id), urls.GetUrl(target.Id, absolute: true), categories);
    }
}
