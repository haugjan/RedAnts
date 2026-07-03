using RedAnts.Domain.Ticketing;

namespace RedAnts.Features.Ticketing.Admin;

/// <summary>Write side for the single catalog field the Anlässe admin table edits inline: the event
/// status. Deliberately narrow. Full catalog CRUD still lives in the Umbraco content tree (the
/// <c>IEvents</c>/<c>CatalogPorts</c> read ports stay read-only); this keeps the status change out of
/// those ports so the catalog contract is not widened.</summary>
public interface IEventStatusEditor
{
    /// <summary>Sets the status on the event's content node and republishes it, so the published
    /// cache (and every catalog reader) reflects the change.</summary>
    Task SetStatusAsync(int eventId, EventStatus status);
}
