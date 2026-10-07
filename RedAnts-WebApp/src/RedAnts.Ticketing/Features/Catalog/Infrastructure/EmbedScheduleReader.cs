using RedAnts.Ticketing.Features.Catalog.Shop;

namespace RedAnts.Ticketing.Features.Catalog.Infrastructure;

public sealed class EmbedScheduleReader(
    IEventReader events,
    IExternalEventReader externalEvents,
    IContentUrls urls) : IEmbedScheduleReader
{
    public async Task<IReadOnlyList<EmbedGame>> GetUpcomingAsync()
    {
        var home = (await events.GetPublicOpenAsync()).Select(e => new EmbedGame(
            e.Name, e.Date, e.StartTime, e.TimeUnknown,
            e.HomeTeamLogoUrl, e.AwayTeamLogoUrl, null,
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
