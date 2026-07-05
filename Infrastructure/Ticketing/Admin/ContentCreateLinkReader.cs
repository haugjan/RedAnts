using Microsoft.Extensions.DependencyInjection;
using RedAnts.Features.Ticketing.Admin;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;
using A = RedAnts.Infrastructure.Ticketing.Content.TicketingAliases;

namespace RedAnts.Infrastructure.Ticketing.Admin;

public sealed class ContentCreateLinkReader(
    IContentTypeService contentTypeService,
    IPublishedContentQuery query,
    IUmbracoContextFactory contextFactory) : IContentCreateLinks
{
    public Task<string?> CreateSeasonUrlAsync()
    {
        using var _ = contextFactory.EnsureUmbracoContext();
        var root = query.ContentAtRoot().FirstOrDefault(c => c.ContentType.Alias == A.RootType);
        var folder = (root?.Children() ?? []).FirstOrDefault(c => c.ContentType.Alias == A.SeasonsFolderType);
        return Task.FromResult(CreateUrl(folder?.Key, A.SeasonType));
    }

    public Task<string?> CreateEventUrlAsync(int seasonId)
    {
        using var _ = contextFactory.EnsureUmbracoContext();
        var season = query.Content(seasonId);
        if (season?.ContentType.Alias != A.SeasonType) return Task.FromResult<string?>(null);
        return Task.FromResult(CreateUrl(season.Key, A.EventType));
    }

    private string? CreateUrl(Guid? parentKey, string docTypeAlias)
    {
        if (parentKey is null) return null;
        var docType = contentTypeService.Get(docTypeAlias);
        if (docType is null) return null;
        return $"/umbraco/section/content/workspace/document/create/parent/document/{parentKey}/{docType.Key}";
    }
}

public sealed class ContentCreateLinkReaderComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IContentCreateLinks, ContentCreateLinkReader>();
}
