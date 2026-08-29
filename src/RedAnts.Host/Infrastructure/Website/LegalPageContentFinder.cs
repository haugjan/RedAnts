using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Extensions;
using A = RedAnts.Infrastructure.Website.WebsiteAliases;

namespace RedAnts.Infrastructure.Website;

public sealed class LegalPageContentFinder(IHttpContextAccessor httpContextAccessor) : IContentFinder
{
    public Task<bool> TryFindContent(IPublishedRequestBuilder request)
    {
        var path = request.AbsolutePathDecoded.TrimEnd('/');
        if (path.Length < 2 || path.IndexOf('/', 1) >= 0)
            return Task.FromResult(false);
        var slug = path[1..];

        var query = httpContextAccessor.HttpContext?.RequestServices.GetService<IPublishedContentQuery>();
        if (query is null) return Task.FromResult(false);

        var roots = query.ContentAtRoot().ToList();
        var legalPages = roots
            .Where(c => c.ContentType.Alias == A.LegalPageType)
            .Concat(roots
                .Where(c => c.ContentType.Alias == A.FooterFolderType)
                .SelectMany(f => f.Children() ?? Enumerable.Empty<IPublishedContent>())
                .Where(c => c.ContentType.Alias == A.LegalPageType));

        var node = legalPages.FirstOrDefault(c =>
            string.Equals(c.Value<string>(A.LegalSlug), slug, StringComparison.OrdinalIgnoreCase));
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
