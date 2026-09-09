using NPoco;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Features.Catalog.Infrastructure;

public sealed class TierSalesReader(IScopeProvider scopeProvider) : ITierSalesReader
{
    public async Task<int> GetSoldCountAsync(int tierId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        return await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM EventTickets WHERE TierId = @0", tierId)
            + await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM SeasonSingleTickets WHERE TierId = @0", tierId)
            + await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM SeasonPasses WHERE TierId = @0", tierId);
    }
}
