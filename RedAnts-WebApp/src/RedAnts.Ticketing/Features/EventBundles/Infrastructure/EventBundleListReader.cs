using NPoco;
using RedAnts.Ticketing.Domain.Sales;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Features.EventBundles.Infrastructure;

public sealed class EventBundleListReader(IScopeProvider scopeProvider) : IEventBundleListReader
{
    public async Task<IReadOnlyList<EventBundleRow>> GetByEventAsync(int eventId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var bundles = await scope.Database.FetchAsync<EventTicketBundleRecord>(
            "WHERE EventId = @0 ORDER BY CreatedAt DESC", eventId);
        if (bundles.Count == 0) return [];

        var counts = await scope.Database.FetchAsync<BundleCountRow>(
            "SELECT BundleId AS BundleId, COUNT(*) AS Total, " +
            "SUM(CASE WHEN Redeemed = 1 THEN 1 ELSE 0 END) AS Redeemed " +
            "FROM EventTickets WHERE EventId = @0 AND BundleId IS NOT NULL GROUP BY BundleId",
            eventId);
        var byBundle = counts.ToDictionary(c => c.BundleId, c => c);

        return bundles.Select(b =>
        {
            var c = byBundle.GetValueOrDefault(b.Id);
            return new EventBundleRow(b.Id, b.EventId, (TicketCategory)b.Category, b.Reference,
                b.CreatedAt, c?.Total ?? 0, c?.Redeemed ?? 0, b.CreatedByName, b.CreatedByEmail);
        }).ToList();
    }

    private sealed class BundleCountRow
    {
        public int BundleId { get; set; }
        public int Total { get; set; }
        public int Redeemed { get; set; }
    }
}
