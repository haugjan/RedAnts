namespace RedAnts.Ticketing.Features.Catalog;

public sealed record SeasonForAdmin(int Id, string Name, DateOnly StartDate, DateOnly EndDate);

public static class GetSeasonsForAdmin
{
    public sealed record Query;

    public sealed class Handler(ISeasons seasons)
    {
        public async Task<IReadOnlyList<SeasonForAdmin>> HandleAsync(Query query) =>
            (await seasons.GetAllAsync())
                .OrderByDescending(s => s.StartDate)
                .Select(s => new SeasonForAdmin(s.Id, s.Name, s.StartDate, s.EndDate))
                .ToList();
    }
}
