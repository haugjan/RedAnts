using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.EventBundles;
using RedAnts.Ticketing.Features.Tickets;

namespace RedAnts.Ticketing.Tests.EventBundles;

internal sealed class RecordingEventTicketBundles : IEventTicketBundles
{
    public HashSet<string> Existing { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<(int EventId, TicketCategory Category, string Reference, int Quantity, int? OrderId)> Created { get; } = [];
    public List<(int EventId, string DefaultBundle)> Imports { get; } = [];

    public Task<IReadOnlyList<EventTicketBundleView>> GetByEventAsync(int eventId) =>
        Task.FromResult<IReadOnlyList<EventTicketBundleView>>([]);

    public Task<bool> ReferenceExistsAsync(int eventId, string reference) => Task.FromResult(Existing.Contains(reference));

    public Task<EventTicketBundleView> CreateAsync(int eventId, TicketCategory category, string reference, int quantity,
        string? createdByName = null, string? createdByEmail = null, int? orderId = null)
    {
        Created.Add((eventId, category, reference, quantity, orderId));
        return Task.FromResult(new EventTicketBundleView(Created.Count, eventId, category, reference, SwissTime.Timestamp, quantity, 0));
    }

    public Task<(int Created, int Updated)> ImportUnifiedAsync(int eventId, IReadOnlyList<TicketImportRow> rows, string defaultBundle,
        TicketCategory defaultCategory, string? createdByName = null, string? createdByEmail = null)
    {
        Imports.Add((eventId, defaultBundle));
        return Task.FromResult((rows.Count, 0));
    }
}
