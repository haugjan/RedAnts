using NPoco;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Features.Orders.Infrastructure;

public sealed class OrderAddOnRepository(IScopeProvider scopeProvider) : IOrderAddOns
{
    public async Task SaveAsync(int orderId, IReadOnlyList<OrderAddOnLine> lines)
    {
        if (lines.Count == 0) return;
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        foreach (var l in lines)
            await scope.Database.InsertAsync(new OrderAddOnRecord
            {
                OrderId = orderId,
                SeasonId = l.SeasonId,
                SeasonName = l.SeasonName,
                Category = (int)l.Category,
                TierId = l.TierId,
                CategoryName = l.CategoryName,
                Label = l.Label,
                Price = l.Price,
                Quantity = l.Quantity
            });
    }

    public async Task SetDeliveredAsync(int orderAddOnId, bool delivered)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await scope.Database.ExecuteAsync(
            "UPDATE OrderAddOns SET Delivered = @0 WHERE Id = @1", delivered, orderAddOnId);
    }
}
