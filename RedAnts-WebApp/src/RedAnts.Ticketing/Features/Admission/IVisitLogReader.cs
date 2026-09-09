using RedAnts.Ticketing.Features.Admission.Admin;

namespace RedAnts.Ticketing.Features.Admission;

public interface IVisitLogReader
{
    Task<IReadOnlyList<TicketVisitEntry>> GetByTicketUuidAsync(Guid uuid);

    Task<IReadOnlyDictionary<Guid, bool>> GetInsideByEventAsync(int eventId);
}
