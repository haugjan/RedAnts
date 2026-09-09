namespace RedAnts.Ticketing.Features.Admission;

public interface IFreeEntryListReader
{
    Task<IReadOnlyList<FreeEntryRow>> GetByEventAsync(int eventId);
}
