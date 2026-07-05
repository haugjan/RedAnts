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

public sealed class SeasonLinkReader(
    IPublishedContentQuery query,
    IPublishedUrlProvider urlProvider,
    IUmbracoContextFactory contextFactory) : ISeasonLinkReader
{
    public Task<IReadOnlyDictionary<int, SeasonLinks>> GetAllAsync()
    {
        using var _ = contextFactory.EnsureUmbracoContext();
        var root = query.ContentAtRoot().FirstOrDefault(c => c.ContentType.Alias == A.RootType);
        var seasonsFolder = (root?.Children() ?? [])
            .FirstOrDefault(c => c.ContentType.Alias == A.SeasonsFolderType);

        IReadOnlyDictionary<int, SeasonLinks> map =
            (seasonsFolder?.Children() ?? [])
            .Where(c => c.ContentType.Alias == A.SeasonType)
            .ToDictionary(s => s.Id, LinksFor);
        return Task.FromResult(map);
    }

    private SeasonLinks LinksFor(IPublishedContent node)
    {
        var url = urlProvider.GetUrl(node.Id, UrlMode.Relative);
        if (string.IsNullOrEmpty(url) || url == "#")
            return new SeasonLinks(null, null);

        var secret = node.Key.ToString().Split('-')[0];
        return new SeasonLinks(url, url.TrimEnd('/') + "?secret=" + secret);
    }
}

public sealed class SeasonLinkReaderComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<ISeasonLinkReader, SeasonLinkReader>();
}
