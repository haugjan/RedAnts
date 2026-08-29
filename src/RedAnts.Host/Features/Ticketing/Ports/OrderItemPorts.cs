using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Ports;

public interface IOrderItems
{
    Task SaveAsync(int orderId, IReadOnlyList<OrderItem> items);

    Task<IReadOnlyList<OrderItem>> GetByOrderAsync(int orderId);
}
