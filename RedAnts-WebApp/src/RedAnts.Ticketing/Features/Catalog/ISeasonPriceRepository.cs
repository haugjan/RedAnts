using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Catalog;

public interface ISeasonPriceRepository
{
    Task<SeasonPrice?> GetBySeasonAsync(int seasonId);
    Task<SeasonPrice> SaveAsync(SeasonPrice price);
    Task DeleteAsync(int seasonPriceId);
    Task SaveReservationAsync(SeasonPrice price);
}
