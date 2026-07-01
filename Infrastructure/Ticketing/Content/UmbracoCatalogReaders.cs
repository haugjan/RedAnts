using RedAnts.Domain.Ticketing;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
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

        var prices = new List<EventPrice>();
        AddPrice(prices, PriceCategory.Child, node.Value<decimal>(A.EventPriceChild));
        AddPrice(prices, PriceCategory.Youth, node.Value<decimal>(A.EventPriceYouth));
        AddPrice(prices, PriceCategory.Adult, node.Value<decimal>(A.EventPriceAdult));

        return Event.FromPersistence(
            node.Id,
            node.Name,
            node.Value<string>(A.EventText),
            node.Parent?.Id ?? 0,
            DateOnly.FromDateTime(start),
            TimeOnly.FromDateTime(start),
            venue?.Id ?? 0,
            TicketingMappers.ParseEnum(node.Value<string>(A.EventStatus) ?? "", EventStatus.Draft),
            node.Value<string>(A.EventInternalLink),
            MediaUrl(node, A.EventImage),
            MediaUrl(node, A.EventHomeTeamLogo),
            MediaUrl(node, A.EventAwayTeamLogo),
            SecretFromKey(node.Key),
            prices);
    }

    private static void AddPrice(List<EventPrice> prices, PriceCategory cat, decimal amount)
    {
        if (amount > 0) prices.Add(new EventPrice(cat, amount));
    }
}

/// <summary>Resolves ticketing nodes from the published content cache.</summary>
internal sealed class CatalogContentSource(IPublishedContentQuery query)
{
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

public sealed class UmbracoSeasons(IPublishedContentQuery query) : ISeasons
{
    private readonly CatalogContentSource _src = new(query);

    public Task<IReadOnlyList<Season>> GetAllAsync() =>
        Task.FromResult<IReadOnlyList<Season>>(_src.Seasons().Select(CatalogContentMapper.ToSeason).ToList());

    public Task<IReadOnlyList<Season>> GetPublicOpenAsync() =>
        Task.FromResult<IReadOnlyList<Season>>(
            _src.Seasons().Select(CatalogContentMapper.ToSeason)
                .Where(s => s.Status == SeasonStatus.Open).ToList());

    public Task<Season?> FindByIdAsync(int id)
    {
        var node = _src.ById(id);
        return Task.FromResult(node?.ContentType.Alias == A.SeasonType ? CatalogContentMapper.ToSeason(node) : null);
    }
}

public sealed class UmbracoVenues(IPublishedContentQuery query) : IVenues
{
    private readonly CatalogContentSource _src = new(query);

    public Task<IReadOnlyList<Venue>> GetAllAsync() =>
        Task.FromResult<IReadOnlyList<Venue>>(_src.Venues().Select(CatalogContentMapper.ToVenue).ToList());

    public Task<Venue?> FindByIdAsync(int id)
    {
        var node = _src.ById(id);
        return Task.FromResult(node?.ContentType.Alias == A.VenueType ? CatalogContentMapper.ToVenue(node) : null);
    }
}

public sealed class UmbracoEvents(IPublishedContentQuery query) : IEvents
{
    private readonly CatalogContentSource _src = new(query);

    public Task<IReadOnlyList<Event>> GetAllAsync() =>
        Task.FromResult<IReadOnlyList<Event>>(_src.Events().Select(CatalogContentMapper.ToEvent).ToList());

    public Task<IReadOnlyList<Event>> GetPublicOpenAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var list = _src.Events().Select(CatalogContentMapper.ToEvent)
            .Where(e => e.Status == EventStatus.Open && e.Date >= today)
            .OrderBy(e => e.Date).ThenBy(e => e.StartTime)
            .ToList();
        return Task.FromResult<IReadOnlyList<Event>>(list);
    }

    public Task<IReadOnlyList<Event>> GetBySeasonAsync(int seasonId)
    {
        var season = _src.ById(seasonId);
        var list = (season?.Children() ?? []).Where(c => c.ContentType.Alias == A.EventType)
            .Select(CatalogContentMapper.ToEvent).ToList();
        return Task.FromResult<IReadOnlyList<Event>>(list);
    }

    public Task<Event?> FindByIdAsync(int id)
    {
        var node = _src.ById(id);
        return Task.FromResult(node?.ContentType.Alias == A.EventType ? CatalogContentMapper.ToEvent(node) : null);
    }
}
