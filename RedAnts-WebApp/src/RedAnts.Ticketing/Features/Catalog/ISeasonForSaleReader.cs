using RedAnts.Ticketing.Features.Catalog.Shop;

namespace RedAnts.Ticketing.Features.Catalog;

public interface ISeasonForSaleReader
{
    Task<SeasonForSale?> FindAsync(int seasonId);

    Task<IReadOnlyList<SeasonPassOffers>> GetPassOffersAsync();
}
