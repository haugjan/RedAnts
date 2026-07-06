using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;
using A = RedAnts.Infrastructure.Ticketing.Content.TicketingAliases;

namespace RedAnts.Infrastructure.Ticketing.Content;

public sealed class SaisonsContentFinder(IPublishedContentQuery query) : IContentFinder
{
    public Task<bool> TryFindContent(IPublishedRequestBuilder request)
    {
        var path = request.AbsolutePathDecoded.TrimEnd('/');
        if (!path.Equals("/saisons", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(false);

        var root = query.ContentAtRoot().FirstOrDefault(c => c.ContentType.Alias == A.RootType);
        var folder = root?.Children().FirstOrDefault(c => c.ContentType.Alias == A.SeasonsFolderType);
        if (folder is null) return Task.FromResult(false);

        request.SetPublishedContent(folder);
        return Task.FromResult(true);
    }
}

public sealed class SaisonsRoutingComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.ContentFinders().Append<SaisonsContentFinder>();
}
