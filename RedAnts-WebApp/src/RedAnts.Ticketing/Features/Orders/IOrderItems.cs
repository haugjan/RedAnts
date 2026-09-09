using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Orders;

public interface IOrderItems
{
    Task SaveAsync(int orderId, IReadOnlyList<OrderItem> items);
}
