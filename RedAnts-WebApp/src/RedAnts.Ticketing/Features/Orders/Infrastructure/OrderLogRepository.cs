using NPoco;
using RedAnts.Ticketing.Domain.Sales;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Features.Orders.Infrastructure;

public sealed class OrderLogRepository(IScopeProvider scopeProvider) : IOrderLog
{
    public async Task AppendAsync(int orderId, OrderStatus toStatus, string? changedBy, string? note = null)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await scope.Database.InsertAsync(new OrderStatusLogRecord
        {
            OrderId = orderId,
            ToStatus = (int)toStatus,
            ChangedBy = string.IsNullOrWhiteSpace(changedBy) ? null : changedBy.Trim(),
            OccurredAt = SwissTime.Timestamp,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        });
    }
}
