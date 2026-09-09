using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Admission;
using RedAnts.Ticketing.Features.Email;
using RedAnts.Ticketing.Features.EventBundles;
using RedAnts.Ticketing.Features.EventBundles.Admin;
using RedAnts.Ticketing.Features.Tickets;

namespace RedAnts.Ticketing.Tests.Tickets;

internal sealed class RecordingTicketDeletion : IAdminTicketDeletion
{
    public List<string> Deleted { get; } = [];

    public Task DeleteEventTicketAsync(Guid uuid) => Record("event", uuid);
    public Task DeleteFlexTicketAsync(Guid uuid) => Record("flex", uuid);
    public Task DeleteSeasonPassAsync(Guid uuid) => Record("pass", uuid);
    public Task DeleteMemberCardAsync(Guid uuid) => Record("member", uuid);

    private Task Record(string kind, Guid uuid)
    {
        Deleted.Add($"{kind}:{uuid}");
        return Task.CompletedTask;
    }
}

internal sealed class StubEventTicketListReader : IEventTicketListReader
{
    public List<EventTicketRow> Rows { get; } = [];
    public List<EventTicketBundleRow> Bundles { get; } = [];

    public Task<IReadOnlyList<EventTicketRow>> GetByEventAsync(int eventId) =>
        Task.FromResult<IReadOnlyList<EventTicketRow>>(Rows.Where(r => r.EventId == eventId).ToList());

    public Task<IReadOnlyList<EventTicketBundleRow>> GetBundlesByEventAsync(int eventId) =>
        Task.FromResult<IReadOnlyList<EventTicketBundleRow>>(Bundles);

    public static EventTicketRow Row(int eventId, string lastName, bool redeemed = false, bool? inside = null, int? bundleId = null,
        string? bundleReference = null, string? email = null) =>
        new(Guid.NewGuid(), "https://tickets.test/ticket/x", eventId, TicketCategory.Adult, 20m, TicketStatus.Valid, redeemed, inside,
            RedemptionStateExtensions.Derive(redeemed, inside), SwissTime.Timestamp, "admin", null, null, bundleId, bundleReference,
            null, null, $"Anna {lastName}",
            CardHolder.Create(BuyerType.Private, null, null, "Anna", lastName, null, email, null, null, null, null, null, null));
}

internal sealed class StubTicketPrintSettings : ITicketPrintSettings
{
    public Dictionary<TicketType, TicketPrintLayout> Layouts { get; } = new();
    public List<(TicketType Type, TicketPrintLayout Layout)> Saved { get; } = [];

    public Task<TicketPrintLayout> GetAsync(TicketType type) =>
        Task.FromResult(Layouts.TryGetValue(type, out var layout) ? layout : TicketPrintLayout.Default);

    public Task SaveAsync(TicketType type, TicketPrintLayout layout)
    {
        Saved.Add((type, layout));
        return Task.CompletedTask;
    }
}

internal sealed class RecordingEventTicketMailer : IEventTicketMailer
{
    public List<(EventMailTicket Ticket, string Subject, string Body)> Sent { get; } = [];

    public string DefaultSubject => "Dein Ticket";

    public string DefaultBody => "Hallo {Name}";

    public Task<EmailSendResult> SendAsync(EventMailTicket ticket, string subject, string body, CancellationToken cancellationToken = default)
    {
        Sent.Add((ticket, subject, body));
        return Task.FromResult(new EmailSendResult(true, null));
    }
}

internal sealed class StubPrintItems : IPrintItemsReader
{
    public List<TicketPrintItem> Items { get; } = [];
    public (TicketType Type, int? BundleId, int? SeasonId, string? Reference, Guid? Uuid)? LastCall { get; private set; }

    public Task<IReadOnlyList<TicketPrintItem>> ResolveAsync(TicketType type, int? bundleId, int? seasonId, string? reference, Guid? uuid)
    {
        LastCall = (type, bundleId, seasonId, reference, uuid);
        return Task.FromResult<IReadOnlyList<TicketPrintItem>>(Items);
    }
}

internal sealed class RecordingTicketPrinter : ITicketPrinter
{
    public List<(IReadOnlyList<TicketPrintItem> Items, TicketPrintLayout Layout)> Builds { get; } = [];

    public Task<byte[]> BuildAsync(IReadOnlyList<TicketPrintItem> items, byte[] templatePdf, TicketPrintLayout layout)
    {
        Builds.Add((items, layout));
        return Task.FromResult(new byte[] { 1, 2, 3 });
    }
}
