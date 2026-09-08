using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Admin;
using RedAnts.Features.Ticketing.Email;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Ticketing.Tests.CardWorkflow;

internal sealed class InMemoryEventTickets : IEventTickets
{
    public List<EventTicket> Stored { get; } = [];
    public List<(Guid Uuid, CardHolder Holder)> Holders { get; } = [];

    public Task<IReadOnlyList<EventTicket>> GetByEventAsync(int eventId) =>
        Task.FromResult<IReadOnlyList<EventTicket>>(Stored.Where(t => t.EventId == eventId).ToList());

    public Task<IReadOnlyList<EventTicket>> GetByOrderAsync(int orderId) =>
        Task.FromResult<IReadOnlyList<EventTicket>>(Stored.Where(t => t.OrderId == orderId).ToList());

    public Task<EventTicket> SaveAsync(EventTicket ticket)
    {
        Stored.RemoveAll(t => t.Uuid == ticket.Uuid);
        Stored.Add(ticket);
        return Task.FromResult(ticket);
    }

    public Task SetHolderAsync(Guid uuid, CardHolder holder)
    {
        Holders.Add((uuid, holder));
        return Task.CompletedTask;
    }
}

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

internal sealed class RecordingFlexBundles : IFlexTicketBundles
{
    public Dictionary<int, FlexTicketBundle> Bundles { get; } = new();
    public HashSet<string> Existing { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<FlexTicketBundle> Saved { get; } = [];
    public List<string> Calls { get; } = [];
    public List<(int SeasonId, string Reference, int Quantity)> Created { get; } = [];

    public Task<IReadOnlyList<FlexTicketBundleView>> GetBySeasonAsync(int seasonId) =>
        Task.FromResult<IReadOnlyList<FlexTicketBundleView>>([]);

    public Task<FlexRebookResult> RebookByUuidAsync(int targetBundleId, Guid uuid, string? operatorName)
    {
        Calls.Add($"rebook-uuid:{targetBundleId}:{uuid}");
        return Task.FromResult(new FlexRebookResult(FlexRebookStatus.Moved));
    }

    public Task<FlexRebookResult> RebookByCodeAsync(int targetBundleId, string codePrefix, string? operatorName)
    {
        Calls.Add($"rebook-code:{targetBundleId}:{codePrefix}");
        return Task.FromResult(new FlexRebookResult(FlexRebookStatus.Moved));
    }

    public Task<FlexBoxOfficeResult> ConvertToBoxOfficeByUuidAsync(Guid uuid, string? operatorName)
    {
        Calls.Add($"boxoffice-uuid:{uuid}");
        return Task.FromResult(new FlexBoxOfficeResult(FlexBoxOfficeStatus.Converted));
    }

    public Task<FlexBoxOfficeResult> ConvertToBoxOfficeByCodeAsync(string codePrefix, string? operatorName)
    {
        Calls.Add($"boxoffice-code:{codePrefix}");
        return Task.FromResult(new FlexBoxOfficeResult(FlexBoxOfficeStatus.Converted));
    }

    public Task<IReadOnlyList<FlexTicketView>> GetTicketsAsync(int bundleId) => Task.FromResult<IReadOnlyList<FlexTicketView>>([]);

    public Task<IReadOnlyList<FlexTicketView>> GetTicketsBySeasonAsync(int seasonId) => Task.FromResult<IReadOnlyList<FlexTicketView>>([]);

    public Task SetTicketStatusAsync(Guid uuid, TicketStatus status)
    {
        Calls.Add($"status:{uuid}:{status}");
        return Task.CompletedTask;
    }

    public Task SetTicketRedeemedAsync(Guid uuid, bool redeemed)
    {
        Calls.Add($"redeemed:{uuid}:{redeemed}");
        return Task.CompletedTask;
    }

    public Task SetTicketCategoryAsync(Guid uuid, TicketCategory category)
    {
        Calls.Add($"category:{uuid}:{category}");
        return Task.CompletedTask;
    }

    public Task<bool> ReferenceExistsAsync(int seasonId, string reference) => Task.FromResult(Existing.Contains(reference));

    public Task<FlexTicketBundleView> CreateAsync(int seasonId, TicketCategory category, string reference, int quantity,
        string? createdByName = null, string? createdByEmail = null, int? orderId = null)
    {
        Created.Add((seasonId, reference, quantity));
        return Task.FromResult(new FlexTicketBundleView(Created.Count, seasonId, category, reference, SwissTime.Timestamp, quantity, 0));
    }

    public Task<FlexTicketBundleView> AddTicketsAsync(int bundleId, TicketCategory category, int quantity,
        string? createdByName = null, string? createdByEmail = null, int? orderId = null)
    {
        Calls.Add($"add:{bundleId}:{quantity}");
        return Task.FromResult(new FlexTicketBundleView(bundleId, 1, category, "x", SwissTime.Timestamp, quantity, 0));
    }

    public Task<FlexTicketBundleView> CreateEmptyAsync(int seasonId, TicketCategory category, string reference,
        string? createdByName = null, string? createdByEmail = null)
    {
        Created.Add((seasonId, reference, 0));
        return Task.FromResult(new FlexTicketBundleView(Created.Count, seasonId, category, reference, SwissTime.Timestamp, 0, 0));
    }

    public Task<bool> DeleteEmptyAsync(int bundleId)
    {
        Calls.Add($"delete-empty:{bundleId}");
        return Task.FromResult(true);
    }

    public Task<(int Created, int Updated)> ImportUnifiedAsync(int seasonId, IReadOnlyList<TicketImportRow> rows, string defaultBundle,
        TicketCategory defaultCategory, string? createdByName = null, string? createdByEmail = null)
    {
        Calls.Add($"import:{seasonId}:{defaultBundle}");
        return Task.FromResult((rows.Count, 0));
    }

    public Task<Guid> CreateSingleAsync(int seasonId, TicketCategory category, string reference, CardHolder holder,
        string? createdByName = null, string? createdByEmail = null)
    {
        Calls.Add($"single:{seasonId}:{reference}");
        return Task.FromResult(Guid.NewGuid());
    }

    public Task SetHolderAsync(Guid uuid, CardHolder holder)
    {
        Calls.Add($"holder:{uuid}");
        return Task.CompletedTask;
    }

    public Task<FlexTicketBundle?> GetByIdAsync(int bundleId) =>
        Task.FromResult(Bundles.TryGetValue(bundleId, out var bundle) ? bundle : null);

    public Task SaveAsync(FlexTicketBundle bundle)
    {
        Saved.Add(bundle);
        return Task.CompletedTask;
    }
}

internal sealed class RecordingTicketDeletion : IAdminTicketDeletion
{
    public List<string> Deleted { get; } = [];

    public Task DeleteEventTicketAsync(Guid uuid) => Record("event", uuid);
    public Task DeleteFlexTicketAsync(Guid uuid) => Record("flex", uuid);
    public Task DeleteSeasonPassAsync(Guid uuid) => Record("pass", uuid);
    public Task DeleteMemberCardAsync(Guid uuid) => Record("member", uuid);
    public Task DeleteFreeEntryAsync(Guid uuid) => Record("free", uuid);

    private Task Record(string kind, Guid uuid)
    {
        Deleted.Add($"{kind}:{uuid}");
        return Task.CompletedTask;
    }
}

internal sealed class RecordingFlexMailer : IFlexTicketMailer
{
    public List<(FlexMailTicket Ticket, string Subject)> Sent { get; } = [];
    public string DefaultSubject => "Dein Flexticket";
    public string DefaultBody => "Hallo";

    public Task<EmailSendResult> SendAsync(FlexMailTicket ticket, string subject, string body, CancellationToken cancellationToken = default)
    {
        Sent.Add((ticket, subject));
        return Task.FromResult(new EmailSendResult(true, null));
    }
}
