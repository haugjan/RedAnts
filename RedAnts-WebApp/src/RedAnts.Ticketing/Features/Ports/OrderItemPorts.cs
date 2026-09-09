using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Ports;

public interface IOrderItems
{
    Task SaveAsync(int orderId, IReadOnlyList<OrderItem> items);

    Task<IReadOnlyList<OrderItem>> GetByOrderAsync(int orderId);
}
