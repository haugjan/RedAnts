using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Checkout;

public interface ICapacityUsageReader
{
    Task<CapacityUsage> GetEventUsageAsync(int eventId);

    Task<CapacityUsage> GetSeasonPassUsageAsync(int seasonId);
}
