using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Catalog;
using RedAnts.Ticketing.Features.Shared;

namespace RedAnts.Ticketing.Features.Orders;

public static class GetOrdersForAdmin
{
    public sealed record Query(int SeasonId, string? Search = null);

    public sealed class Handler(IOrderListReader orders, IEvents events)
    {
        public async Task<OrdersForAdmin> HandleAsync(Query query)
        {
            if (query.SeasonId <= 0) return OrdersForAdmin.Empty;
            var eventIds = (await events.GetBySeasonAsync(query.SeasonId)).Select(e => e.Id).ToList();
            var rows = await orders.GetBySeasonAsync(query.SeasonId, eventIds);
            var terms = SearchTerms.Parse(query.Search);
            var shown = terms.Length == 0 ? rows : rows.Where(o => SearchTerms.Matches(Haystack(o), terms)).ToList();
            return new OrdersForAdmin(rows.Count, shown);
        }

        private static string Haystack(OrderListRow o) => string.Join(' ', new[]
        {
            o.OrderNumber, o.BuyerName, o.Email, o.Street, o.AddressLine2 ?? "", o.PostalCode, o.City, o.Country,
            o.EventTicketSummary, o.SeasonPassSummary, o.FlexTicketSummary,
            o.PaymentSource is { } ps ? ps.DisplayName() : "", o.Status.DisplayName()
        });
    }
}
