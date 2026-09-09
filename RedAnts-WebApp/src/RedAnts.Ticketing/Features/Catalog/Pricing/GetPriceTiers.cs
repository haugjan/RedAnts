namespace RedAnts.Ticketing.Features.Catalog.Pricing;

public sealed record PriceTierRow(int Id, string Name, int? MinAge, int? MaxAge, int SortOrder, int? PromoOfTierId);

public static class GetPriceTiers
{
    public sealed record Query(int SeasonId);

    public sealed class Handler(ISeasonsForAdminReader reader)
    {
        public Task<IReadOnlyList<PriceTierRow>> HandleAsync(Query query) => reader.GetTiersAsync(query.SeasonId);
    }
}
