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
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using A = RedAnts.Infrastructure.Ticketing.Content.TicketingAliases;

namespace RedAnts.Infrastructure.Ticketing.Admin;

public sealed class UmbracoEventContentEditor(IContentService contentService) : IEventContentEditor
{
    private const int SuperUser = Constants.Security.SuperUserId;

    public Task SetNameAsync(int eventId, string name) => Task.Run(() =>
    {
        var trimmed = (name ?? "").Trim();
        if (trimmed.Length == 0) throw new DomainException("Bezeichnung darf nicht leer sein.");

        var node = Load(eventId);
        node.Name = trimmed;
        Publish(node);
    });

    public Task SetStartAsync(int eventId, DateOnly date, TimeOnly time) => Task.Run(() =>
    {
        var node = Load(eventId);
        node.SetValue(A.EventStart, date.ToDateTime(time));
        Publish(node);
    });

    private IContent Load(int eventId)
    {
        var node = contentService.GetById(eventId)
            ?? throw new DomainException($"Anlass {eventId} wurde nicht gefunden.");
        if (node.ContentType.Alias != A.EventType)
            throw new DomainException($"Inhalt {eventId} ist kein Anlass.");
        return node;
    }

    private void Publish(IContent node)
    {
        contentService.Save(node, SuperUser);
        contentService.Publish(node, new[] { "*" }, SuperUser);
    }
}

public sealed class EventContentEditorComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IEventContentEditor, UmbracoEventContentEditor>();
}
