namespace RedAnts.Ticketing.Features.Admission;

public interface IEventAdmissionReader
{
    Task<IReadOnlyDictionary<int, EventAdmissionCounts>> GetCountsByEventAsync();
}
