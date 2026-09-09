using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Catalog;

public interface ISeasonAddOnRepository
{
    Task<SeasonAddOnSet> LoadSeasonAsync(int seasonId);
    Task ReplaceForSeasonAsync(int seasonId, IReadOnlyList<SeasonAddOn> options);
}
