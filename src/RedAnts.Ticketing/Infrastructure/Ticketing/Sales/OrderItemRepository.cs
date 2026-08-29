using NPoco;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Sales;

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

    public async Task<IReadOnlyList<OrderItem>> GetByOrderAsync(int orderId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<OrderItemRecord>(
            "WHERE OrderId = @0 ORDER BY Id", orderId);
        return rows.Select(r => OrderItem.FromPersistence(
            r.Id, r.OrderId, (OrderItemKind)r.Kind, r.ArticleGuid, r.RefId,
            (TicketCategory)r.Category, r.Label, r.Quantity, r.UnitPrice)).ToList();
    }
}
