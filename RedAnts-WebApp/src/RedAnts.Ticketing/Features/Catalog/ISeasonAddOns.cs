using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Catalog;

public interface ISeasonAddOns
{
    Task<IReadOnlyList<SeasonAddOn>> GetBySeasonAsync(int seasonId);
    Task ReplaceForSeasonAsync(int seasonId, IReadOnlyList<SeasonAddOn> options);
}
