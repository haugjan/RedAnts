namespace RedAnts.Ticketing.Features.Admission;

public interface IVisitLogReader
{
    Task<IReadOnlyList<TicketVisitEntry>> GetByTicketUuidAsync(Guid uuid);
}
