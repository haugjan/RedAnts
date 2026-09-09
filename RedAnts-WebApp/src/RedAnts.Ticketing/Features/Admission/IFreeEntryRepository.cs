using RedAnts.Ticketing.Domain.Admission;
using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Admission;

public interface IFreeEntryRepository
{
    Task<FreeEntryQuota> GetQuotaAsync(int eventId);
    Task SaveQuotaAsync(int eventId, FreeEntryQuota quota);
    Task<int> CountGrantedAsync(int eventId, FreeEntryType type);
    Task<FreeEntry?> FindLatestInsideAsync(int eventId, FreeEntryType type);
    Task SaveAsync(FreeEntry entry);
}
