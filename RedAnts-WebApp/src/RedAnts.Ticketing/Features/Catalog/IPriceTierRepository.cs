using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Catalog;

public interface IPriceTierRepository
{
    Task<IReadOnlyList<PriceTier>> GetBySeasonAsync(int seasonId);
    Task<IReadOnlyList<PriceTier>> SaveForSeasonAsync(int seasonId, IReadOnlyList<PriceTierInput> tiers);
}

public sealed record PriceTierInput(int Id, string Name, int? MinAge, int? MaxAge, int SortOrder, PriceTierPromoInput? Promo);

public sealed record PriceTierPromoInput(int Id, string Name);
