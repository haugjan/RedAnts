using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Catalog;

public interface IEventPrices
{
    Task<EventPrice?> GetByEventAsync(int eventId);
    Task<EventPrice> SaveAsync(EventPrice price);
    Task DeleteAsync(int eventPriceId);
    Task<CapacityUsage> GetUsageAsync(int eventId);
    Task SaveReservationAsync(EventPrice price);
}
