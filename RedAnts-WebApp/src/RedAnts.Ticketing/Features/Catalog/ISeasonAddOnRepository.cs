using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Catalog;

public interface ISeasonAddOnRepository
{
    Task<IReadOnlyList<SeasonAddOn>> GetBySeasonAsync(int seasonId);
    Task ReplaceForSeasonAsync(int seasonId, IReadOnlyList<SeasonAddOn> options);
}
