namespace RedAnts.Ticketing.Features.SeasonPasses;

public static class GetSeasonPasses
{
    public sealed record Query(int SeasonId, string? Bundle = null, string? Search = null);

    public sealed record Result(IReadOnlyList<SeasonPassRow> Passes, IReadOnlyList<string> Bundles, int Total);

    public sealed class Handler(ISeasonPassListReader reader)
    {
        public async Task<Result> HandleAsync(Query query)
        {
            var rows = await reader.GetBySeasonAsync(query.SeasonId);
            var bundles = rows.Where(p => !string.IsNullOrEmpty(p.Reference)).Select(p => p.Reference!).Distinct().Order().ToList();
            var byBundle = string.IsNullOrEmpty(query.Bundle)
                ? rows
                : rows.Where(p => string.Equals(p.Reference, query.Bundle, StringComparison.Ordinal));
            var terms = (query.Search ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var passes = terms.Length == 0
                ? byBundle.ToList()
                : byBundle.Where(p => terms.All(t => Haystack(p).Contains(t, StringComparison.OrdinalIgnoreCase))).ToList();
            return new Result(passes, bundles, rows.Count);
        }

        private static string Haystack(SeasonPassRow p) => string.Join(' ', new[]
        {
            p.CardNo, p.BuyerName ?? "", p.Email ?? "", p.Reference ?? "",
            p.CategoryName, p.StatusLabel, p.PaymentState ?? "",
            p.OrderNumber ?? "", p.Price.ToString("N2")
        });
    }
}
