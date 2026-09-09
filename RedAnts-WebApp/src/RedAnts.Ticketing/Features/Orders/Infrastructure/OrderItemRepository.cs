using NPoco;
using RedAnts.Ticketing.Domain.Sales;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Features.Orders.Infrastructure;

public sealed class OrderItemRepository(IScopeProvider scopeProvider) : IOrderItems
{
    public async Task SaveAsync(int orderId, IReadOnlyList<OrderItem> items)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        foreach (var item in items)
        {
            await db.InsertAsync(new OrderItemRecord
            {
                OrderId = orderId,
                Kind = (int)item.Kind,
                ArticleGuid = item.ArticleGuid,
                RefId = item.RefId,
                Category = (int)item.Category,
                Label = item.Label,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });
        }
    }
}
