using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Orders;

public interface IOrderRepository
{
    Task<Order> SaveAsync(Order order);
    Task<string> NextOrderNumberAsync();
    Task<Order?> GetByIdAsync(int id);
    Task<Order?> GetByNumberAsync(string orderNumber);
    Task<bool> TryMarkPaidAsync(int orderId);
    Task<bool> TryCancelDraftAsync(int orderId);
    Task CopyBillingToTicketsAsync(int orderId);
}
