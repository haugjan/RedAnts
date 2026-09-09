using NPoco;
using RedAnts.Ticketing.Domain.Sales;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Features.Orders.Infrastructure;

public sealed class DraftOrdersReader(IScopeProvider scopeProvider) : IDraftOrdersReader
{
    public async Task<IReadOnlyList<int>> GetIdsCreatedBetweenAsync(DateTimeOffset createdAfter, DateTimeOffset createdBefore)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        return await scope.Database.FetchAsync<int>(
            "SELECT Id FROM Orders WHERE Status = @0 AND CreatedAt >= @1 AND CreatedAt < @2 ORDER BY Id",
            (int)OrderStatus.Draft, createdAfter, createdBefore);
    }
}
