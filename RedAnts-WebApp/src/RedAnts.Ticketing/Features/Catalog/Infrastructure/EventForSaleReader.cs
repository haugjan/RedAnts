using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Features.Catalog.Shop;
using RedAnts.Ticketing.Features.Checkout;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using A = RedAnts.Ticketing.Infrastructure.Content.TicketingAliases;

namespace RedAnts.Ticketing.Features.Catalog.Infrastructure;

public sealed class EventForSaleReader(
    IPublishedContentQuery query,
    IUmbracoContextFactory contextFactory,
    IEventPricing pricing,
    IEventConversionRuleReader conversionRules,
    IContentUrls urls) : IEventForSaleReader
{
    private readonly CatalogContentSource _src = new(query, contextFactory);

    public async Task<EventForSale?> FindAsync(int eventId)
    {
        var content = _src.Read(() =>
        {
            var node = _src.ById(eventId);
            if (node is null || node.ContentType.Alias != A.EventType) return null;
            var venue = node.Value<IPublishedContent>(A.EventVenue);
            return new EventContent(CatalogContentMapper.ToEvent(node), venue is null ? null : ToVenue(venue));
        });
        if (content is null) return null;

        var categories = await pricing.GetAvailableAsync(eventId);
        var rules = await conversionRules.GetByEventAsync(eventId);
        var conversionOnly = await conversionRules.GetConversionOnlyAsync(eventId);
        var e = content.Event;
        return new EventForSale(e.Id, e.Name, e.Text, e.Date, e.StartTime, e.TimeUnknown, e.ImageUrl, e.HomeTeamLogoUrl, e.AwayTeamLogoUrl,
            content.Venue, urls.GetUrl(e.Id), categories, conversionOnly, rules.Count > 0);
    }

    private static VenueForSale ToVenue(IPublishedContent node) =>
        new(node.Id, node.Name, node.Value<string>(A.VenueGoogleGeoId)?.Trim(), node.Value<IPublishedContent>(A.VenueImage)?.Url(),
            node.Value<string>(A.VenueArrival), node.Value<string>(A.VenueAddress)?.Trim());

    private sealed record EventContent(Event Event, VenueForSale? Venue);
}
