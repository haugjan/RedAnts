namespace RedAnts.Ticketing.Features.Catalog.Shop;

public sealed record SeasonForSale(
    int Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string? ImageUrl,
    string? Url,
    IReadOnlyList<UpcomingEvent> Events);

public static class GetSeasonForSale
{
    public sealed record Query(int SeasonId);

    public sealed class Handler(ISeasonForSaleReader reader)
    {
        public Task<SeasonForSale?> HandleAsync(Query query) => reader.FindAsync(query.SeasonId);
    }
}
