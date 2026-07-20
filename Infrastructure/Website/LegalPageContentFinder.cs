using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Routing;
using Umbraco.Extensions;
using A = RedAnts.Infrastructure.Website.WebsiteAliases;

namespace RedAnts.Infrastructure.Website;

public sealed class LegalPageContentFinder(IHttpContextAccessor httpContextAccessor) : IContentFinder
{
    private static readonly Dictionary<string, string> Routes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/impressum"] = "impressum",
        ["/datenschutz"] = "datenschutz"
    };

    public Task<bool> TryFindContent(IPublishedRequestBuilder request)
    {
        var path = request.AbsolutePathDecoded.TrimEnd('/');
        if (!Routes.TryGetValue(path, out var slug))
            return Task.FromResult(false);

        var query = httpContextAccessor.HttpContext?.RequestServices.GetService<IPublishedContentQuery>();
        if (query is null) return Task.FromResult(false);

        var node = query.ContentAtRoot().FirstOrDefault(c =>
            c.ContentType.Alias == A.LegalPageType
            && string.Equals(c.Value<string>(A.LegalSlug), slug, StringComparison.OrdinalIgnoreCase));
        if (node is null) return Task.FromResult(false);

        request.SetPublishedContent(node);
        return Task.FromResult(true);
    }
}

public sealed class LegalPageRoutingComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.ContentFinders().Append<LegalPageContentFinder>();
}
