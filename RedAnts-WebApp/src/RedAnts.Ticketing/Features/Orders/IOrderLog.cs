using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Orders;

public interface IOrderLog
{
    Task AppendAsync(int orderId, OrderStatus toStatus, string? changedBy, string? note = null);
}
