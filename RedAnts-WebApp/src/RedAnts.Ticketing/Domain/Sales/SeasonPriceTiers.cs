namespace RedAnts.Ticketing.Domain.Sales;

public sealed record SeasonPriceTiers(int SeasonId, IReadOnlyList<PriceTier> Tiers)
{
    public IEnumerable<PriceTier> MainTiers => Tiers.Where(t => !t.IsPromo);

    public int BaseTierIdOf(int tierId) => Tiers.FirstOrDefault(t => t.Id == tierId)?.PromoOfTierId ?? tierId;
}
