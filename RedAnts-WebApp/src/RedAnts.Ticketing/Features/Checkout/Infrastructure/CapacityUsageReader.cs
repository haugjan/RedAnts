using NPoco;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Catalog.Infrastructure;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Features.Checkout.Infrastructure;

public sealed class CapacityUsageReader(IScopeProvider scopeProvider) : ICapacityUsageReader
{
    public Task<CapacityUsage> GetEventUsageAsync(int eventId) =>
        UsageAsync("EventTickets", "EventId", eventId);

    public Task<CapacityUsage> GetSeasonPassUsageAsync(int seasonId) =>
        UsageAsync("SeasonPasses", "SeasonId", seasonId);

    private async Task<CapacityUsage> UsageAsync(string table, string scopeColumn, int scopeId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var byTier = await scope.Database.FetchAsync<TierCountRow>(
            $"SELECT TierId AS TierId, COUNT(*) AS Cnt FROM {table} WHERE {scopeColumn} = @0 AND Status = @1 AND TierId IS NOT NULL GROUP BY TierId",
            scopeId, (int)TicketStatus.Valid);
        var total = await scope.Database.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM {table} WHERE {scopeColumn} = @0 AND Status = @1", scopeId, (int)TicketStatus.Valid);
        return new CapacityUsage(total, byTier.ToDictionary(r => r.TierId, r => r.Cnt));
    }
}
