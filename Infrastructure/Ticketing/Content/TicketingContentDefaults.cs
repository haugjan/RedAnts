using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using A = RedAnts.Infrastructure.Ticketing.Content.TicketingAliases;

namespace RedAnts.Infrastructure.Ticketing.Content;

public sealed class TicketingContentDefaultsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AddNotificationHandler<ContentSavedNotification, TicketingContentDefaults>();
}

/// <summary>
/// After save of an event/season node: (re)computes the read-only public and internal link display
/// fields from the node's sqid and its secret (the first block of the node's GUID key).
/// <para>
/// This runs on <see cref="ContentSavedNotification"/> (not ContentSaving) because the link encodes
/// the node's integer id, which is only assigned once the node has been persisted. For a brand-new
/// node the id is not yet available during ContentSaving, so the values would stay empty. When the
/// computed values differ from what is stored we write them and save once more; the follow-up save
/// finds the values already correct and stops, so there is no save loop.
/// </para>
/// </summary>
public sealed class TicketingContentDefaults(IContentService contentService, ISqidEncoder sqids)
    : INotificationHandler<ContentSavedNotification>
{
    public void Handle(ContentSavedNotification notification)
    {
        foreach (var entity in notification.SavedEntities)
        {
            var alias = entity.ContentType.Alias;
            var isEvent = alias == A.EventType;
            var isSeason = alias == A.SeasonType;
            if (!isEvent && !isSeason) continue;

            var secret = entity.Key.ToString().Split('-')[0]; // first GUID block = 8 hex chars
            var basePath = isEvent ? "/tickets/event/" : "/saisonkarten/";
            var sqid = sqids.Encode(entity.Id);
            var publicLink = $"{basePath}{sqid}";
            var internLink = $"{basePath}{sqid}?secret={secret}";

            // Only write (and re-save) when something actually changed, so the save triggered below
            // does not recurse indefinitely.
            if (entity.GetValue<string>(A.PublicLink) == publicLink &&
                entity.GetValue<string>(A.InternLink) == internLink)
                continue;

            entity.SetValue(A.PublicLink, publicLink);
            entity.SetValue(A.InternLink, internLink);
            contentService.Save(entity);
        }
    }
}
