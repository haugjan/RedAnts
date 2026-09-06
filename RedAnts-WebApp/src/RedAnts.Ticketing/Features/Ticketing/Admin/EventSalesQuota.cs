namespace RedAnts.Features.Ticketing.Admin;

public interface IEventSalesQuota
{
    Task<IReadOnlyDictionary<int, int?>> GetAllAsync();

    Task SetAsync(int eventId, int? salesQuota);
}
