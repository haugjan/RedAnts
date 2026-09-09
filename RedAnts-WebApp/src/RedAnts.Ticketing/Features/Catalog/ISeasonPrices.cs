using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Catalog;

public interface ISeasonPrices
{
    Task<SeasonPrice?> GetBySeasonAsync(int seasonId);
    Task<SeasonPrice> SaveAsync(SeasonPrice price);
    Task DeleteAsync(int seasonPriceId);
    Task<CapacityUsage> GetPassUsageAsync(int seasonId);
    Task SaveReservationAsync(SeasonPrice price);
}
