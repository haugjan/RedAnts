using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Admin;
using RedAnts.Ticketing.Features.Admission.Admin;
using RedAnts.Ticketing.Features.Shared;

namespace RedAnts.Ticketing.Features.Tickets;

public static class GetEventTicketsForAdmin
{
    public sealed record Query(int EventId, int? BundleId = null, string? Search = null);

    public sealed class Handler(IEventTicketListReader tickets)
    {
        public async Task<EventTicketsForAdmin> HandleAsync(Query query)
        {
            if (query.EventId <= 0) return EventTicketsForAdmin.Empty;
            var rows = await tickets.GetByEventAsync(query.EventId);
            var bundles = await tickets.GetBundlesByEventAsync(query.EventId);
            var terms = SearchTerms.Parse(query.Search);
            var shown = rows
                .Where(t => query.BundleId is not > 0 || t.BundleId == query.BundleId)
                .Where(t => terms.Length == 0 || SearchTerms.Matches(Haystack(t), terms))
                .ToList();
            return new EventTicketsForAdmin(
                rows.Count,
                rows.Count(t => t.Redemption == RedemptionState.Redeemed),
                rows.Count(t => t.Redemption == RedemptionState.Outside),
                rows.Count(t => t.Redemption == RedemptionState.Open),
                shown,
                bundles);
        }

        private static string Haystack(EventTicketRow t)
        {
            var h = t.Holder;
            return string.Join(' ', new[]
            {
                AdminFormat.TicketNo(t.Uuid), h?.Company ?? "", h?.FirstName ?? "", h?.LastName ?? "", h?.Email ?? "",
                t.Category.DisplayName(), t.Status.DisplayName(), t.BundleReference ?? "", t.Price.ToString("N2")
            });
        }
    }
}
