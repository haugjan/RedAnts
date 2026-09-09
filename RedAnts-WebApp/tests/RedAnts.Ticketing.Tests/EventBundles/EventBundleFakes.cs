using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.EventBundles;
using RedAnts.Ticketing.Features.Tickets;

namespace RedAnts.Ticketing.Tests.EventBundles;

internal sealed class RecordingEventTicketBundles : IEventTicketBundleRepository
{
    public HashSet<string> Existing { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<(int EventId, TicketCategory Category, string Reference, int Quantity, int? OrderId)> Created { get; } = [];
    public List<(int EventId, string DefaultBundle)> Imports { get; } = [];

    public Task<bool> ReferenceExistsAsync(int eventId, string reference) => Task.FromResult(Existing.Contains(reference));

    public Task<int> CreateAsync(int eventId, TicketCategory category, string reference, int quantity,
        string? createdByName = null, string? createdByEmail = null, int? orderId = null)
    {
        Created.Add((eventId, category, reference, quantity, orderId));
        return Task.FromResult(Created.Count);
    }

    public bool ImportThrows { get; set; }

    public Task<(int Created, int Updated)> ImportUnifiedAsync(int eventId, IReadOnlyList<TicketImportRow> rows, string defaultBundle,
        TicketCategory defaultCategory, string? createdByName = null, string? createdByEmail = null)
    {
        if (ImportThrows) throw new InvalidOperationException("event ticket table unavailable");
        Imports.Add((eventId, defaultBundle));
        return Task.FromResult((rows.Count, 0));
    }
}

internal sealed class FakeEventBundleListReader : IEventBundleListReader
{
    public List<EventBundleRow> Rows { get; } = [];
    public List<int> Requested { get; } = [];

    public Task<IReadOnlyList<EventBundleRow>> GetByEventAsync(int eventId)
    {
        Requested.Add(eventId);
        return Task.FromResult<IReadOnlyList<EventBundleRow>>(Rows.Where(b => b.EventId == eventId).ToList());
    }
}

internal sealed class FakeEventBundleTicketsReader : IEventBundleTicketsReader
{
    public Dictionary<int, List<EventBundleTicketRow>> ByBundle { get; } = new();
    public List<string> Calls { get; } = [];

    public Task<IReadOnlyList<EventBundleTicketRow>> GetByBundleAsync(int bundleId)
    {
        Calls.Add($"bundle:{bundleId}");
        return Task.FromResult<IReadOnlyList<EventBundleTicketRow>>(ByBundle.GetValueOrDefault(bundleId) ?? []);
    }

    public Task<IReadOnlyList<EventBundleTicketRow>> GetByBundlesAsync(IReadOnlyCollection<int> bundleIds)
    {
        Calls.Add($"bundles:{string.Join(',', bundleIds)}");
        return Task.FromResult<IReadOnlyList<EventBundleTicketRow>>(bundleIds.SelectMany(id => ByBundle.GetValueOrDefault(id) ?? []).ToList());
    }
}
