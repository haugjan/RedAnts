using RedAnts.Ticketing.Features.Catalog.Pricing;

namespace RedAnts.Ticketing.Features.Catalog;

public interface ISeasonsForAdminReader
{
    Task<SeasonsForAdmin> GetAllAsync();

    Task<IReadOnlyList<SeasonChoice>> GetChoicesAsync();

    Task<IReadOnlyList<PriceTierRow>> GetTiersAsync(int seasonId);

    Task<IReadOnlyList<SeasonTierPrice>> GetTierPricesAsync(int seasonId);

    Task<IReadOnlyList<SeasonAddOnRow>> GetAddOnsAsync(int seasonId);
}
