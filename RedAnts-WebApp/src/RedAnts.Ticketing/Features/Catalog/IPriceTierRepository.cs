using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Catalog;

public interface IPriceTierRepository
{
    Task<SeasonPriceTiers> LoadSeasonAsync(int seasonId);
    Task<SeasonPriceTiers> SaveForSeasonAsync(int seasonId, IReadOnlyList<PriceTierInput> tiers);
}

public sealed record PriceTierInput(int Id, string Name, int? MinAge, int? MaxAge, int SortOrder, PriceTierPromoInput? Promo);

public sealed record PriceTierPromoInput(int Id, string Name);
