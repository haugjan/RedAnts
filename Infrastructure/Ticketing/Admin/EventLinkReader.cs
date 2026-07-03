using Microsoft.Extensions.DependencyInjection;
using RedAnts.Features.Ticketing.Admin;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;
using A = RedAnts.Infrastructure.Ticketing.Content.TicketingAliases;

namespace RedAnts.Infrastructure.Ticketing.Admin;

/// <summary>Reads the public/internal share links off the published event nodes of a season. The links
/// are label properties on the node (computed on publish), so this just projects them; it does not build
/// URLs itself. Ensures an ambient Umbraco context because it runs on the Blazor admin circuit.</summary>
public sealed class EventLinkReader(IPublishedContentQuery query, IUmbracoContextFactory contextFactory)
    : IEventLinkReader
{
    public Task<IReadOnlyDictionary<int, EventLinks>> GetBySeasonAsync(int seasonId)
    {
        using var _ = contextFactory.EnsureUmbracoContext();
        var season = query.Content(seasonId);
        IReadOnlyDictionary<int, EventLinks> map =
            (season?.Children() ?? [])
            .Where(c => c.ContentType.Alias == A.EventType)
            .ToDictionary(
                e => e.Id,
                e => new EventLinks(e.Value<string>(A.PublicLink), e.Value<string>(A.InternLink)));
        return Task.FromResult(map);
    }
}

/// <summary>Registers the Anlässe link reader (auto-discovered via <c>.AddComposers()</c>).</summary>
public sealed class EventLinkReaderComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IEventLinkReader, EventLinkReader>();
}
