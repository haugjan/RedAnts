namespace RedAnts.Ticketing.Features.MemberCards;

public static class GetMemberCardsForExport
{
    public sealed record Query(int SeasonId, IReadOnlyList<string> Bundles);

    public sealed class Handler(IMemberCardListReader reader)
    {
        public async Task<IReadOnlyList<MemberCardRow>> HandleAsync(Query query)
        {
            var rows = await reader.GetBySeasonAsync(query.SeasonId);
            return query.Bundles.Count == 0
                ? rows
                : rows.Where(c => query.Bundles.Contains(c.Reference ?? "", StringComparer.Ordinal)).ToList();
        }
    }
}
