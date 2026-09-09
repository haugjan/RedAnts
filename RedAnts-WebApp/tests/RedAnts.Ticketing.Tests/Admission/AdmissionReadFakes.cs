using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Admission;
using RedAnts.Ticketing.Features.Catalog;
using RedAnts.Ticketing.Features.Tickets;

namespace RedAnts.Ticketing.Tests.Admission;

internal sealed class StubFreeEntryList : IFreeEntryListReader
{
    public List<FreeEntryRow> Rows { get; } = [];
    public int? LastEventId { get; private set; }

    public Task<IReadOnlyList<FreeEntryRow>> GetByEventAsync(int eventId)
    {
        LastEventId = eventId;
        return Task.FromResult<IReadOnlyList<FreeEntryRow>>(Rows);
    }
}

internal sealed class StubVisitLog : IVisitLogReader
{
    public Dictionary<Guid, IReadOnlyList<TicketVisitEntry>> Visits { get; } = new();

    public Task<IReadOnlyList<TicketVisitEntry>> GetByTicketUuidAsync(Guid uuid) =>
        Task.FromResult(Visits.TryGetValue(uuid, out var entries) ? entries : []);
}

internal sealed class StubEventAdmission : IEventAdmissionReader
{
    public Dictionary<int, EventAdmissionCounts> Counts { get; } = new();

    public Task<IReadOnlyDictionary<int, EventAdmissionCounts>> GetCountsByEventAsync() =>
        Task.FromResult<IReadOnlyDictionary<int, EventAdmissionCounts>>(Counts);
}

internal sealed class StubVenues : IVenues
{
    public List<Venue> Venues { get; } = [];

    public Task<IReadOnlyList<Venue>> GetAllAsync() => Task.FromResult<IReadOnlyList<Venue>>(Venues);

    public Task<Venue?> FindByIdAsync(int id) => Task.FromResult(Venues.FirstOrDefault(v => v.Id == id));
}

internal sealed class VerifyingTicketTokens : ITicketTokens
{
    public Dictionary<string, TicketTokenData> Tokens { get; } = new();
    public Dictionary<string, string> ShortCodes { get; } = new();

    public string Create(TicketType type, Guid uuid, int scopeId) => throw new NotSupportedException();

    public bool TryVerify(string token, out TicketTokenData data) => Tokens.TryGetValue(token, out data!);

    public string CreateShort(Guid uuid) => throw new NotSupportedException();

    public bool TryVerifyShort(string token, out string code) => ShortCodes.TryGetValue(token, out code!);
}
