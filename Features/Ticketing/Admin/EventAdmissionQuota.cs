namespace RedAnts.Features.Ticketing.Admin;

public interface IEventAdmissionQuota
{
    Task<IReadOnlyDictionary<int, int?>> GetAllAsync();

    Task SetAsync(int eventId, int? admissionQuota);
}
