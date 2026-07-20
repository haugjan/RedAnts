using NPoco;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Sales;

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

    public async Task<IReadOnlyList<OrderAddOnLine>> GetByOrderAsync(int orderId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<OrderAddOnRecord>(
            "WHERE OrderId = @0 ORDER BY Id", orderId);
        return rows.Select(r => new OrderAddOnLine(
            r.SeasonId, r.SeasonName, (TicketCategory)r.Category, r.CategoryName, r.Label, r.Price, r.Quantity, r.TierId)).ToList();
    }
}
