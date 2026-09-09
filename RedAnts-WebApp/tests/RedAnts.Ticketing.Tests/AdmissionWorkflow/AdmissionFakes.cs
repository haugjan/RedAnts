using RedAnts.Ticketing.Domain.Admission;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Ports;
using RedAnts.Ticketing.Features.Tickets;

namespace RedAnts.Ticketing.Tests.AdmissionWorkflow;

internal sealed class StubAdmissionFacts : IAdmissionFactsReader
{
    public Dictionary<Guid, AdmissionFacts> Facts { get; } = [];
    public int Reads { get; private set; }

    public Task<AdmissionFacts> ReadAsync(int eventId, TicketType ticketType, Guid ticketUuid)
    {
        Reads++;
        return Task.FromResult(Facts.TryGetValue(ticketUuid, out var facts)
            ? facts
            : new AdmissionFacts(null, null, null, false, false, null, null));
    }
}

internal sealed class InMemoryAdmissions : IAdmissionRepository
{
    private long _nextVisitId = 1;
    private readonly Dictionary<(int EventId, Guid Uuid), Admission> _stored = [];

    public int Saves { get; private set; }

    public Task<Admission> LoadAsync(int eventId, TicketType ticketType, Guid ticketUuid) =>
        Task.FromResult(_stored.TryGetValue((eventId, ticketUuid), out var admission)
            ? admission
            : Admission.None(eventId, ticketType, ticketUuid));

    public Task SaveAsync(Admission admission)
    {
        Saves++;
        foreach (var visit in admission.Visits)
            if (visit.IsNew || visit.Changed)
                visit.MarkPersisted(visit.IsNew ? _nextVisitId++ : visit.Id);
        _stored[(admission.EventId, admission.TicketUuid)] = admission;
        return Task.CompletedTask;
    }

    public Admission? Stored(int eventId, Guid uuid) => _stored.GetValueOrDefault((eventId, uuid));
}

internal sealed class RecordingRedemptions : ITicketRedemptions
{
    public List<(TicketType Type, Guid Uuid, int EventId)> Marked { get; } = [];

    public Task MarkRedeemedAsync(TicketType ticketType, Guid ticketUuid, int eventId)
    {
        Marked.Add((ticketType, ticketUuid, eventId));
        return Task.CompletedTask;
    }
}

internal sealed class StubOccupancy : IOccupancyReader
{
    public Occupancy Current { get; set; } = new(10, 100);

    public Task<Occupancy> GetAsync(int eventId) => Task.FromResult(Current);
}

internal sealed class InMemoryFreeEntries : IFreeEntryRepository
{
    public Dictionary<int, FreeEntryQuota> SavedQuotas { get; } = new();

    public Task SaveQuotaAsync(int eventId, FreeEntryQuota quota)
    {
        SavedQuotas[eventId] = quota;
        return Task.CompletedTask;
    }

    private long _nextVisitId = 500;

    public FreeEntryQuota Quota { get; set; } = FreeEntryQuota.Unlimited;
    public List<FreeEntry> Stored { get; } = [];

    public Task<FreeEntryQuota> GetQuotaAsync(int eventId) => Task.FromResult(Quota);

    public Task<int> CountGrantedAsync(int eventId, FreeEntryType type) =>
        Task.FromResult(Stored.Count(e => e.EventId == eventId && e.Type == type));

    public Task<FreeEntry?> FindLatestInsideAsync(int eventId, FreeEntryType type) =>
        Task.FromResult(Stored.LastOrDefault(e => e.EventId == eventId && e.Type == type && e.IsInside));

    public Task SaveAsync(FreeEntry entry)
    {
        if (entry.IsNew)
        {
            entry.MarkPersisted(_nextVisitId++);
            Stored.Add(entry);
        }
        else
        {
            entry.MarkPersisted(entry.VisitId);
        }
        return Task.CompletedTask;
    }
}

internal sealed class StubIssuedTickets : IIssuedTicketReader
{
    public Dictionary<Guid, IssuedTicket> Tickets { get; } = [];

    public Task<IssuedTicket?> FindAsync(Guid uuid) => Task.FromResult(Tickets.GetValueOrDefault(uuid));

    public Task<IssuedTicket?> FindByCodeAsync(string codePrefix) =>
        Task.FromResult(Tickets.Values.FirstOrDefault(t => t.Uuid.ToString("N").StartsWith(codePrefix, StringComparison.OrdinalIgnoreCase)));
}
