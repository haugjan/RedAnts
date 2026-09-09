using RedAnts.Ticketing.Features.Admission.Admin;

namespace RedAnts.Ticketing.Features.Admission;

public interface IFreeEntryAdminReport
{
    Task<IReadOnlyList<FreeEntryListItem>> GetByEventAsync(int eventId);
}
