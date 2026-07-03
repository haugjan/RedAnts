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

/// <summary>
/// After save of an event/season node: (re)computes the read-only public and internal link display
/// fields. The public link is the node's official Umbraco content URL (e.g.
/// <c>/saisons/saison-202627/red-ants-vs-laupen/</c>); the internal link is that same URL plus the
/// <c>?secret=</c> query parameter (secret = first block of the node's GUID key).
/// <para>
/// Runs on <see cref="ContentSavedNotification"/>; the URL is only available once the node is
/// persisted/published, so it stays empty for a brand-new unpublished node and fills on the next save.
/// Writes (and re-saves) only when a value actually changed, so there is no save loop.
/// </para>
/// </summary>
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

        // URL generation needs an ambient UmbracoContext (absent during some save operations).
        using var _ = umbracoContextFactory.EnsureUmbracoContext();
        foreach (var entity in nodes)
        {
            if (!TicketingLinks.TryApply(entity, urlProvider)) continue;
            contentService.Save(entity);
        }
    }
}

/// <summary>Shared computation of the public/internal link display fields from the Umbraco content URL.</summary>
internal static class TicketingLinks
{
    /// <summary>Sets PublicLink/InternLink on the node if they changed. Returns true when a value changed.</summary>
    public static bool TryApply(IContent node, IPublishedUrlProvider urlProvider)
    {
        var url = urlProvider.GetUrl(node.Id, UrlMode.Relative);
        if (string.IsNullOrEmpty(url) || url == "#") return false; // not published yet -> no public URL

        var secret = node.Key.ToString().Split('-')[0]; // first GUID block = 8 hex chars
        var publicLink = url;
        var internLink = url.TrimEnd('/') + "?secret=" + secret;

        if (node.GetValue<string>(A.PublicLink) == publicLink && node.GetValue<string>(A.InternLink) == internLink)
            return false;

        node.SetValue(A.PublicLink, publicLink);
        node.SetValue(A.InternLink, internLink);
        return true;
    }
}
