using NPoco;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Features.Checkout.Infrastructure;

public sealed class CheckoutOrderReader(IScopeProvider scopeProvider) : ICheckoutOrderReader
{
    public async Task<int?> FindIdByNumberAsync(string orderNumber)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        return await scope.Database.ExecuteScalarAsync<int?>("SELECT TOP 1 Id FROM Orders WHERE OrderNumber = @0", orderNumber);
    }
}
