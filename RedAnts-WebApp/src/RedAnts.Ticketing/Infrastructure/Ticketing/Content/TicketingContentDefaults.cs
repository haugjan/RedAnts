using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;
using A = RedAnts.Infrastructure.Ticketing.Content.TicketingAliases;

namespace RedAnts.Infrastructure.Ticketing.Content;

public sealed class TicketingContentDefaultsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AddNotificationHandler<ContentSavedNotification, TicketingContentDefaults>();
}

public sealed class TicketingContentDefaults(
    IContentService contentService,
    IPublishedUrlProvider urlProvider,
    IUmbracoContextFactory umbracoContextFactory)
    : INotificationHandler<ContentSavedNotification>
{
    public void Handle(ContentSavedNotification notification)
    {
        var nodes = notification.SavedEntities
            .Where(e => e.ContentType.Alias is A.EventType or A.SeasonType).ToList();
        if (nodes.Count == 0) return;

        using var _ = umbracoContextFactory.EnsureUmbracoContext();
        foreach (var entity in nodes)
        {
            if (!TicketingLinks.TryApply(entity, urlProvider)) continue;
            contentService.Save(entity);
        }
    }
}

internal static class TicketingLinks
{
    public static bool TryApply(IContent node, IPublishedUrlProvider urlProvider)
    {
        var url = urlProvider.GetUrl(node.Id, UrlMode.Relative);
        if (string.IsNullOrEmpty(url) || url == "#") return false;

        var secret = node.Key.ToString().Split('-')[0];
        var publicLink = url;
        var internLink = url.TrimEnd('/') + "?secret=" + secret;

        if (node.GetValue<string>(A.PublicLink) == publicLink && node.GetValue<string>(A.InternLink) == internLink)
            return false;

        node.SetValue(A.PublicLink, publicLink);
        node.SetValue(A.InternLink, internLink);
        return true;
    }
}
