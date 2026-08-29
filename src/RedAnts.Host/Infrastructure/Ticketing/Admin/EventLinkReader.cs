using Microsoft.Extensions.DependencyInjection;
using RedAnts.Features.Ticketing.Admin;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;
using A = RedAnts.Infrastructure.Ticketing.Content.TicketingAliases;

namespace RedAnts.Infrastructure.Ticketing.Admin;

public sealed class EventLinkReader(
    IPublishedContentQuery query,
    IPublishedUrlProvider urlProvider,
    IUmbracoContextFactory contextFactory) : IEventLinkReader
{
    public Task<IReadOnlyDictionary<int, EventLinks>> GetBySeasonAsync(int seasonId)
    {
        using var _ = contextFactory.EnsureUmbracoContext();
        var season = query.Content(seasonId);
        IReadOnlyDictionary<int, EventLinks> map =
            (season?.Children() ?? [])
            .Where(c => c.ContentType.Alias == A.EventType)
            .ToDictionary(e => e.Id, LinksFor);
        return Task.FromResult(map);
    }

    private EventLinks LinksFor(IPublishedContent node)
    {
        var url = urlProvider.GetUrl(node.Id, UrlMode.Relative);
        if (string.IsNullOrEmpty(url) || url == "#")
            return new EventLinks(null, null);

        var secret = node.Key.ToString().Split('-')[0];
        return new EventLinks(url, url.TrimEnd('/') + "?secret=" + secret);
    }
}

public sealed class EventLinkReaderComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IEventLinkReader, EventLinkReader>();
}
