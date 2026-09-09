using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Shared;

namespace RedAnts.Ticketing.Features.Orders;

public static class GetOrderAddOnsForAdmin
{
    public sealed record Query(int SeasonId, string? Search = null, bool OnlyOpen = false);

    public sealed class Handler(IOrderAddOnListReader addOns)
    {
        public async Task<OrderAddOnsForAdmin> HandleAsync(Query query)
        {
            if (query.SeasonId <= 0) return OrderAddOnsForAdmin.Empty;
            var rows = await addOns.GetBySeasonAsync(query.SeasonId);
            var terms = SearchTerms.Parse(query.Search);
            var shown = rows
                .Where(r => !query.OnlyOpen || !r.Delivered)
                .Where(r => terms.Length == 0 || SearchTerms.Matches(Haystack(r), terms))
                .ToList();
            return new OrderAddOnsForAdmin(rows.Count, rows.Count(r => r.Delivered), rows.Sum(r => r.Quantity), shown);
        }

        private static string Haystack(OrderAddOnRow r) => string.Join(' ', new[]
        {
            r.OrderNumber, r.BuyerName, r.Email, r.Label, r.CategoryName, r.OrderStatus.DisplayName()
        });
    }
}
