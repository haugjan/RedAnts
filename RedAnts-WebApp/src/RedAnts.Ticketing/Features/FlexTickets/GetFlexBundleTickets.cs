namespace RedAnts.Ticketing.Features.FlexTickets;

public static class GetFlexBundleTickets
{
    public sealed record Query(int SeasonId, int? BundleId = null, string? Search = null);

    public sealed record Result(IReadOnlyList<FlexTicketRow> Tickets, int Total);

    public sealed class Handler(IFlexBundleTicketsReader reader)
    {
        public async Task<Result> HandleAsync(Query query)
        {
            var rows = query.BundleId is > 0 and var bundleId
                ? await reader.GetByBundleAsync(bundleId)
                : await reader.GetBySeasonAsync(query.SeasonId);
            var terms = (query.Search ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var tickets = terms.Length == 0
                ? rows
                : rows.Where(t => terms.All(term => Haystack(t).Contains(term, StringComparison.OrdinalIgnoreCase))).ToList();
            return new Result(tickets, rows.Count);
        }

        private static string Haystack(FlexTicketRow t)
        {
            var h = t.Holder;
            return string.Join(' ', new[]
            {
                t.CardNo, h?.Company ?? "", h?.FirstName ?? "", h?.LastName ?? "", h?.Email ?? "",
                t.CategoryLabel, t.StatusLabel
            });
        }
    }
}
