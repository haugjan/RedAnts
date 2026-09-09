using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Orders;

public interface IOrders
{
    Task<Order> SaveAsync(Order order);
    Task<string> NextOrderNumberAsync();
    Task<Order?> GetByIdAsync(int id);
    Task<Order?> GetByNumberAsync(string orderNumber);
    Task<bool> TryMarkPaidAsync(int orderId);
    Task<bool> TryCancelDraftAsync(int orderId);
    Task<IReadOnlyList<Order>> GetDraftsCreatedBetweenAsync(DateTimeOffset createdAfter, DateTimeOffset createdBefore);
    Task CopyBillingToTicketsAsync(int orderId);
}
