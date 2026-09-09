namespace RedAnts.Ticketing.Features.Catalog;

public sealed record SeasonChoice(int Id, string Name, DateOnly StartDate, DateOnly EndDate, bool IsCurrent);

public static class GetSeasonChoices
{
    public sealed record Query;

    public sealed class Handler(ISeasonsForAdminReader reader)
    {
        public Task<IReadOnlyList<SeasonChoice>> HandleAsync(Query query) => reader.GetChoicesAsync();
    }
}
