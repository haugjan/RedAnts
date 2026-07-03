using Microsoft.Extensions.DependencyInjection;
using RedAnts.Domain.Ticketing;
using RedAnts.Features.Ticketing.Admin;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Services;
using A = RedAnts.Infrastructure.Ticketing.Content.TicketingAliases;

namespace RedAnts.Infrastructure.Ticketing.Admin;

/// <summary>Updates the status field on an event's Umbraco content node and republishes it. The
/// status is an <c>Umbraco.DropDown.Flexible</c>, persisted as a JSON array holding the selected
/// value (e.g. <c>["Open"]</c>); the <see cref="EventStatus"/> enum name is exactly that value, so
/// the write mirrors how the seeder sets it. Save+Publish matches the seeder's publish helper.</summary>
public sealed class UmbracoEventStatusEditor(IContentService contentService) : IEventStatusEditor
{
    private const int SuperUser = Constants.Security.SuperUserId;

    // IContentService.Save/Publish are synchronous and internally block on async work. Called directly
    // from a Blazor Server event handler they run on the circuit's SynchronizationContext, which
    // sync-over-async deadlocks — the save just hangs on "Speichert …" with no exception. Running the
    // whole operation via Task.Run moves it onto a threadpool thread without that context, so it
    // completes normally; exceptions still surface through the returned task to the caller's try/catch.
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

/// <summary>Registers the event-status write port (auto-discovered via <c>.AddComposers()</c>).</summary>
public sealed class EventStatusEditorComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IEventStatusEditor, UmbracoEventStatusEditor>();
}
