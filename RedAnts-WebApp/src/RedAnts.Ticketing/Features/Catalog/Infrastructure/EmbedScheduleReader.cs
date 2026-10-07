using RedAnts.Ticketing.Features.Catalog.Shop;

namespace RedAnts.Ticketing.Features.Catalog.Infrastructure;

public sealed class EmbedScheduleReader(
    IEventReader events,
    IExternalEventReader externalEvents,
    IVenueReader venues,
    IContentUrls urls) : IEmbedScheduleReader
{
    public async Task<IReadOnlyList<EmbedGame>> GetUpcomingAsync()
    {
        var venueNames = new VenueNames(venues);
        var home = new List<EmbedGame>();
        foreach (var e in await events.GetPublicOpenAsync())
            home.Add(new EmbedGame(
                e.Name, e.Date, e.StartTime, e.TimeUnknown,
                e.HomeTeamLogoUrl, e.AwayTeamLogoUrl, await venueNames.ForAsync(e.VenueId),
                urls.GetUrl(e.Id, absolute: true), false));

        var away = (await externalEvents.GetUpcomingAsync()).Select(x => new EmbedGame(
            x.Name, x.Date, x.StartTime, x.TimeUnknown,
            x.HomeTeamLogoUrl, x.AwayTeamLogoUrl, x.Location,
            null, true));

        return home.Concat(away)
            .OrderBy(g => g.Date).ThenBy(g => g.StartTime)
            .ToList();
    }
}
