using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Admin;

public sealed record TicketVisitScan(VisitLogType Type, DateTime OccurredAt, string? ScannedBy);

public sealed record TicketVisitEntry(
    long VisitId,
    int EventId,
    string EventName,
    DateOnly? EventDate,
    bool IsInside,
    IReadOnlyList<TicketVisitScan> Scans);

public interface IVisitLogReader
{
    Task<IReadOnlyList<TicketVisitEntry>> GetByTicketUuidAsync(Guid uuid);
}
