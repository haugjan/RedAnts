using RedAnts.Ticketing.Features.Catalog.Shop;
using RedAnts.Ticketing.Features.Checkout;

namespace RedAnts.Ticketing.Features.Catalog.Infrastructure;

public sealed class TicketingHomeReader(
    IEventReader events,
    IExternalEventReader externalEvents,
    ISeasonReader seasons,
    IVenueReader venues,
    IEventPricing pricing,
    ISeasonForSaleReader seasonOffers,
    IContentUrls urls) : ITicketingHomeReader
{
    public async Task<TicketingHome> GetAsync()
    {
        var openSeasonIds = (await seasons.GetPublicOpenAsync()).Select(s => s.Id).ToHashSet();
        var venueNames = new VenueNames(venues);
        var upcoming = new List<UpcomingEvent>();
        foreach (var e in (await events.GetPublicOpenAsync()).Where(e => openSeasonIds.Contains(e.SeasonId)))
        {
            var item = SaleOffers.ToUpcoming(e, await venueNames.ForAsync(e.VenueId), urls.GetUrl(e.Id), await pricing.GetAvailableAsync(e.Id));
            if (item.Buyable || item.SoldOut) upcoming.Add(item);
        }

        foreach (var x in (await externalEvents.GetUpcomingAsync()).Where(x => openSeasonIds.Contains(x.SeasonId)))
            upcoming.Add(new UpcomingEvent(
                x.Id, x.Name, x.Date, x.StartTime, x.TimeUnknown, x.Location, null,
                x.HomeTeamLogoUrl, x.AwayTeamLogoUrl, null, false, false, true));

        var ordered = upcoming.OrderBy(e => e.Date).ThenBy(e => e.StartTime).ToList();
        var passOffers = (await seasonOffers.GetPassOffersAsync()).Where(s => s.Offers.Count > 0).ToList();
        return new TicketingHome(ordered, passOffers);
    }
}
