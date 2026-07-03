using RedAnts.Domain.Ticketing;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;
using A = RedAnts.Infrastructure.Ticketing.Content.TicketingAliases;

namespace RedAnts.Infrastructure.Ticketing.Content;

/// <summary>Shared mapping from published content nodes to ticketing read models.</summary>
internal static class CatalogContentMapper
{
    /// <summary>The Intern access secret is the first block of the node's GUID key (stored implicitly, no edit field).</summary>
    public static string SecretFromKey(Guid key) => key.ToString().Split('-')[0];

    // Base categories that the legacy (Stage 1) purchase flow still understands.
    private static readonly Dictionary<string, PriceCategory> LegacyCategoryByCode =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["child"] = PriceCategory.Child,
            ["youth"] = PriceCategory.Youth,
            ["adult"] = PriceCategory.Adult,
        };

    public static Season ToSeason(IPublishedContent node) =>
        Season.FromPersistence(
            node.Id,
            node.Name,
            DateOnly.FromDateTime(node.Value<DateTime>(A.SeasonStartDate)),
            DateOnly.FromDateTime(node.Value<DateTime>(A.SeasonEndDate)),
            TicketingMappers.ParseEnum(node.Value<string>(A.SeasonStatus) ?? "", SeasonStatus.Draft),
            MediaUrl(node, A.SeasonImage),
            SecretFromKey(node.Key),
            ReadSalesPrices(node));

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

        var salesPrices = ReadSalesPrices(node);
        // Stage 1 compatibility: expose the base categories (child/youth/adult) as the legacy
        // EventPrice list so the existing single-ticket purchase flow keeps working. Reduced
        // variants are display-only until the purchase flow is migrated (Stage 2).
        var prices = salesPrices
            .Where(p => p.Price > 0 && LegacyCategoryByCode.ContainsKey(p.CategoryCode))
            .Select(p => new EventPrice(LegacyCategoryByCode[p.CategoryCode], p.Price))
            .ToList();

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
            SecretFromKey(node.Key),
            prices,
            salesPrices);
    }

    /// <summary>Reads the "salesPrices" Block List into resolved <see cref="TicketPrice"/> rows.
    /// Effective price = category default price when the row uses it, otherwise the row's own price.</summary>
    private static IReadOnlyList<TicketPrice> ReadSalesPrices(IPublishedContent node)
    {
        var blocks = node.Value<BlockListModel>(A.SalesPrices);
        if (blocks is null || blocks.Count == 0) return [];

        var list = new List<TicketPrice>();
        foreach (var block in blocks)
        {
            var content = block.Content;
            var category = content.Value<IPublishedContent>(A.SalesPriceCategory);
            if (category is null) continue;

            var code = category.Value<string>(A.CategoryCode) ?? "";
            var useDefault = content.Value<bool>(A.SalesPriceUseDefault);
            var price = useDefault
                ? category.Value<decimal>(A.CategoryDefaultPrice)
                : content.Value<decimal>(A.SalesPricePrice);
            var contingentRaw = content.Value<int>(A.SalesPriceContingent);
            int? contingent = contingentRaw > 0 ? contingentRaw : null;

            list.Add(new TicketPrice(code, category.Name, price, contingent));
        }
        return list;
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

    public Task<IReadOnlyList<Event>> GetUpcomingForScanningAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var list = _src.Events().Select(CatalogContentMapper.ToEvent)
            .Where(e => e.Date >= today)
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
