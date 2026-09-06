// Uses Umbraco 17 APIs deprecated for removal in Umbraco 18 (content/data-type Save, DataType GetAll,
// FileService templates, Constants.Security.SuperUserId, IPublishedContent.Parent, SpecialDbTypes.NTEXT).
// Still functional; migrate to the async management services at the Umbraco 18 upgrade.
#pragma warning disable CS0618
using Microsoft.Extensions.DependencyInjection;
using RedAnts.Domain.Ticketing;
using RedAnts.Features.Ticketing.Admin;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Services;
using A = RedAnts.Infrastructure.Ticketing.Content.TicketingAliases;

namespace RedAnts.Infrastructure.Ticketing.Admin;

public sealed class UmbracoEventStatusEditor(IContentService contentService) : IEventStatusEditor
{
    private const int SuperUser = Constants.Security.SuperUserId;

    public Task SetStatusAsync(int eventId, EventStatus status) => Task.Run(() =>
    {
        var node = contentService.GetById(eventId)
            ?? throw new InvalidOperationException($"Anlass {eventId} wurde nicht gefunden.");
        if (node.ContentType.Alias != A.EventType)
            throw new InvalidOperationException($"Inhalt {eventId} ist kein Anlass.");

        node.SetValue(A.EventStatus, System.Text.Json.JsonSerializer.Serialize(new[] { status.ToString() }));
        contentService.Save(node, SuperUser);
        contentService.Publish(node, new[] { "*" }, SuperUser);
    });
}

public sealed class EventStatusEditorComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IEventStatusEditor, UmbracoEventStatusEditor>();
}
