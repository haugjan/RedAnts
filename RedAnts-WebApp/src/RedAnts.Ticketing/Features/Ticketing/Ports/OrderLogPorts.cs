using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Ports;

public sealed record OrderLogEntry(OrderStatus ToStatus, string? ChangedBy, DateTimeOffset OccurredAt, string? Note);

public interface IOrderLog
{
    Task AppendAsync(int orderId, OrderStatus toStatus, string? changedBy, string? note = null);

    Task<IReadOnlyList<OrderLogEntry>> GetByOrderAsync(int orderId);
}
