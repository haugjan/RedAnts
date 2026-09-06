using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Routing;
using Umbraco.Extensions;
using A = RedAnts.Infrastructure.Ticketing.Content.TicketingAliases;

namespace RedAnts.Infrastructure.Ticketing.Content;

public sealed class SaisonsContentFinder(IHttpContextAccessor httpContextAccessor) : IContentFinder
{
    public Task<bool> TryFindContent(IPublishedRequestBuilder request)
    {
        var path = request.AbsolutePathDecoded.TrimEnd('/');
        if (!path.Equals("/seasons", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(false);

        var query = httpContextAccessor.HttpContext?.RequestServices.GetService<IPublishedContentQuery>();
        if (query is null) return Task.FromResult(false);

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
