using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;

namespace RedAnts.Infrastructure.Ticketing.Content;

public sealed class UmbracoContentUrls(IPublishedUrlProvider urlProvider, IUmbracoContextFactory contextFactory) : IContentUrls
{
    public string? GetUrl(int nodeId, bool absolute = false)
    {
        using var _ = contextFactory.EnsureUmbracoContext();
        var url = urlProvider.GetUrl(nodeId, absolute ? UrlMode.Absolute : UrlMode.Relative);
        return !string.IsNullOrEmpty(url) && url != "#" ? url : null;
    }
}
