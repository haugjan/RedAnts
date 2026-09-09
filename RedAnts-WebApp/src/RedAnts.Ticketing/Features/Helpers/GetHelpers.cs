namespace RedAnts.Ticketing.Features.Helpers;

public static class GetHelpers
{
    public sealed record Query(int SeasonId);

    public sealed class Handler(IHelperListReader reader)
    {
        public Task<IReadOnlyList<HelperRow>> HandleAsync(Query query) => reader.GetBySeasonAsync(query.SeasonId);
    }
}
