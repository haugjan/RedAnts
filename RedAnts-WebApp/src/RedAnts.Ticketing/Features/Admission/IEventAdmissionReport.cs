using RedAnts.Ticketing.Features.Admission.Admin;

namespace RedAnts.Ticketing.Features.Admission;

public interface IEventAdmissionReport
{
    Task<IReadOnlyDictionary<int, EventAdmissionCounts>> GetCountsByEventAsync();
}
