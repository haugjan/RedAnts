namespace RedAnts.Ticketing.Features.SeasonPasses;

public static class GetSeasonPassesForExport
{
    public sealed record Query(int SeasonId, IReadOnlyList<string> Bundles);

    public sealed class Handler(ISeasonPassListReader reader)
    {
        public async Task<IReadOnlyList<SeasonPassRow>> HandleAsync(Query query)
        {
            var rows = await reader.GetBySeasonAsync(query.SeasonId);
            return query.Bundles.Count == 0
                ? rows
                : rows.Where(p => query.Bundles.Contains(p.Reference ?? "", StringComparer.Ordinal)).ToList();
        }
    }
}
