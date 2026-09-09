using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.EventBundles;

namespace RedAnts.Ticketing.Features.Tickets;

public static class GetTicketsForExport
{
    public sealed record Query(IReadOnlyCollection<int> BundleIds);

    public sealed class Handler(IEventBundleTickets bundleTickets, ITicketTokens tokens, IPublicBaseUrl publicUrl)
    {
        public async Task<IReadOnlyList<TicketExportRow>> HandleAsync(Query query)
        {
            var tickets = await bundleTickets.GetByBundlesAsync(query.BundleIds);
            return tickets.Select(t => new TicketExportRow(
                t.Uuid.ToString("N")[..8].ToUpperInvariant(), t.Reference, t.Category.DisplayName(),
                t.Holder ?? CardHolder.Empty, null, publicUrl.TicketUrl(tokens.CreateShort(t.Uuid)))).ToList();
        }
    }
}
