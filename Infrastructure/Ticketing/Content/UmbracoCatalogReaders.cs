using RedAnts.Domain.Ticketing;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;
using A = RedAnts.Infrastructure.Ticketing.Content.TicketingAliases;

namespace RedAnts.Infrastructure.Ticketing.Content;

/// <summary>Shared mapping from published content nodes to ticketing read models.</summary>
internal static class CatalogContentMapper
{
    /// <summary>The Intern access secret is the first block of the node's GUID key (stored implicitly, no edit field).</summary>
    public static string SecretFromKey(Guid key) => key.ToString().Split('-')[0];

    public static Season ToSeason(IPublishedContent node) =>
        Season.FromPersistence(
            node.Id,
            node.Name,
            DateOnly.FromDateTime(node.Value<DateTime>(A.SeasonStartDate)),
            DateOnly.FromDateTime(node.Value<DateTime>(A.SeasonEndDate)),
            TicketingMappers.ParseEnum(node.Value<string>(A.SeasonStatus) ?? "", SeasonStatus.Draft),
            MediaUrl(node, A.SeasonImage),
            SecretFromKey(node.Key));

    public static Venue ToVenue(IPublishedContent node) =>
        Venue.FromPersistence(
            node.Id,
            node.Name,
            node.Value<string>(A.VenueGoogleGeoId),
            MediaUrl(node, A.VenueImage),
            node.Value<string>(A.VenueDescription));

    /// <summary>Resolves a single MediaPicker3 value to its URL, or null.</summary>
    private static string? MediaUrl(IPublishedContent node, string alias) =>
        node.Value<IPublishedContent>(alias)?.Url();

    public static Event ToEvent(IPublishedContent node)
    {
        var start = node.Value<DateTime>(A.EventStart);
        var venue = node.Value<IPublishedContent>(A.EventVenue);

        return Event.FromPersistence(
            node.Id,
            node.Name,
            node.Value<string>(A.EventText),
            node.Parent?.Id ?? 0,
            DateOnly.FromDateTime(start),
            TimeOnly.FromDateTime(start),
            venue?.Id ?? 0,
            TicketingMappers.ParseEnum(node.Value<string>(A.EventStatus) ?? "", EventStatus.Draft),
            MediaUrl(node, A.EventImage),
            MediaUrl(node, A.EventHomeTeamLogo),
            MediaUrl(node, A.EventAwayTeamLogo),
            SecretFromKey(node.Key));
    }
}

/// <summary>Resolves ticketing nodes from the published content cache.</summary>
internal sealed class CatalogContentSource(IPublishedContentQuery query, IUmbracoContextFactory contextFactory)
{
    /// <summary>
    /// Ensures an ambient <c>UmbracoContext</c> exists for the duration of <paramref name="read"/>.
    /// Needed because these readers are also called from the Blazor admin circuit (over SignalR), which
    /// has no request-scoped Umbraco context. <c>EnsureUmbracoContext</c> is nest-safe, so the normal
    /// MVC request path (which already has a context) is unaffected.
    /// </summary>
    public T Read<T>(Func<T> read)
    {
        using var _ = contextFactory.EnsureUmbracoContext();
        return read();
    }

    public IPublishedContent? Root() =>
        query.ContentAtRoot().FirstOrDefault(c => c.ContentType.Alias == A.RootType);

    public IPublishedContent? ById(int id) => query.Content(id);

    private IPublishedContent? Folder(string alias) =>
        (Root()?.Children() ?? []).FirstOrDefault(c => c.ContentType.Alias == alias);

    public IEnumerable<IPublishedContent> Seasons() =>
        (Folder(A.SeasonsFolderType)?.Children() ?? []).Where(c => c.ContentType.Alias == A.SeasonType);

    public IEnumerable<IPublishedContent> Venues() =>
        (Folder(A.VenuesFolderType)?.Children() ?? []).Where(c => c.ContentType.Alias == A.VenueType);

    public IEnumerable<IPublishedContent> Events() =>
        Seasons().SelectMany(s => (s.Children() ?? []).Where(c => c.ContentType.Alias == A.EventType));
}

public sealed class UmbracoSeasons(IPublishedContentQuery query, IUmbracoContextFactory contextFactory) : ISeasons
{
    private readonly CatalogContentSource _src = new(query, contextFactory);

    public Task<IReadOnlyList<Season>> GetAllAsync() =>
        Task.FromResult(_src.Read<IReadOnlyList<Season>>(
            () => _src.Seasons().Select(CatalogContentMapper.ToSeason).ToList()));

    public Task<IReadOnlyList<Season>> GetPublicOpenAsync() =>
        Task.FromResult(_src.Read<IReadOnlyList<Season>>(
            () => _src.Seasons().Select(CatalogContentMapper.ToSeason)
                .Where(s => s.Status == SeasonStatus.Open).ToList()));

    public Task<Season?> FindByIdAsync(int id) =>
        Task.FromResult(_src.Read(() =>
        {
            var node = _src.ById(id);
            return node?.ContentType.Alias == A.SeasonType ? CatalogContentMapper.ToSeason(node) : null;
        }));
}

public sealed class UmbracoVenues(IPublishedContentQuery query, IUmbracoContextFactory contextFactory) : IVenues
{
    private readonly CatalogContentSource _src = new(query, contextFactory);

    public Task<IReadOnlyList<Venue>> GetAllAsync() =>
        Task.FromResult(_src.Read<IReadOnlyList<Venue>>(
            () => _src.Venues().Select(CatalogContentMapper.ToVenue).ToList()));

    public Task<Venue?> FindByIdAsync(int id) =>
        Task.FromResult(_src.Read(() =>
        {
            var node = _src.ById(id);
            return node?.ContentType.Alias == A.VenueType ? CatalogContentMapper.ToVenue(node) : null;
        }));
}

public sealed class UmbracoEvents(IPublishedContentQuery query, IUmbracoContextFactory contextFactory) : IEvents
{
    private readonly CatalogContentSource _src = new(query, contextFactory);

    public Task<IReadOnlyList<Event>> GetAllAsync() =>
        Task.FromResult(_src.Read<IReadOnlyList<Event>>(
            () => _src.Events().Select(CatalogContentMapper.ToEvent).ToList()));

    public Task<IReadOnlyList<Event>> GetPublicOpenAsync() =>
        Task.FromResult(_src.Read<IReadOnlyList<Event>>(() =>
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            return _src.Events().Select(CatalogContentMapper.ToEvent)
                .Where(e => e.Status == EventStatus.Open && e.Date >= today)
                .OrderBy(e => e.Date).ThenBy(e => e.StartTime)
                .ToList();
        }));

    public Task<IReadOnlyList<Event>> GetUpcomingForScanningAsync() =>
        Task.FromResult(_src.Read<IReadOnlyList<Event>>(() =>
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            return _src.Events().Select(CatalogContentMapper.ToEvent)
                .Where(e => e.Date >= today)
                .OrderBy(e => e.Date).ThenBy(e => e.StartTime)
                .ToList();
        }));

    public Task<IReadOnlyList<Event>> GetBySeasonAsync(int seasonId) =>
        Task.FromResult(_src.Read<IReadOnlyList<Event>>(() =>
        {
            var season = _src.ById(seasonId);
            return (season?.Children() ?? []).Where(c => c.ContentType.Alias == A.EventType)
                .Select(CatalogContentMapper.ToEvent)
                .OrderBy(e => e.Date).ThenBy(e => e.StartTime)
                .ToList();
        }));

    public Task<Event?> FindByIdAsync(int id) =>
        Task.FromResult(_src.Read(() =>
        {
            var node = _src.ById(id);
            return node?.ContentType.Alias == A.EventType ? CatalogContentMapper.ToEvent(node) : null;
        }));
}
