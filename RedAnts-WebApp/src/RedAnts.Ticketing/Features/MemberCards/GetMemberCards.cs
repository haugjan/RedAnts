namespace RedAnts.Ticketing.Features.MemberCards;

public static class GetMemberCards
{
    public sealed record Query(int SeasonId, string? Bundle = null, string? Search = null);

    public sealed record Result(IReadOnlyList<MemberCardRow> Cards, IReadOnlyList<string> Bundles,
        IReadOnlyList<MemberCardMailBatch> MailBatches, int Total);

    public sealed class Handler(IMemberCardListReader reader)
    {
        public async Task<Result> HandleAsync(Query query)
        {
            var rows = await reader.GetBySeasonAsync(query.SeasonId);
            var bundles = rows.Where(c => !string.IsNullOrWhiteSpace(c.Reference)).Select(c => c.Reference!).Distinct().Order().ToList();
            var batches = rows
                .Where(c => !c.IsCancelled && !string.IsNullOrWhiteSpace(c.Reference))
                .GroupBy(c => (Reference: c.Reference!, c.Category))
                .OrderBy(g => g.Key.Reference).ThenBy(g => g.Key.Category)
                .Select(g => new MemberCardMailBatch(g.Key.Reference, g.Key.Category, g.ToList()))
                .ToList();
            var byBundle = string.IsNullOrEmpty(query.Bundle)
                ? rows
                : rows.Where(c => string.Equals(c.Reference, query.Bundle, StringComparison.Ordinal));
            var terms = (query.Search ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var cards = terms.Length == 0
                ? byBundle.ToList()
                : byBundle.Where(c => terms.All(t => Haystack(c).Contains(t, StringComparison.OrdinalIgnoreCase))).ToList();
            return new Result(cards, bundles, batches, rows.Count);
        }

        private static string Haystack(MemberCardRow c) => string.Join(' ', new[]
        {
            c.CardNo, c.FirstName ?? "", c.LastName ?? "", c.Address?.Company ?? "",
            c.Email ?? "", c.Reference ?? "", c.CategoryLabel, c.StatusLabel
        });
    }
}
