using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Admin;

public interface IOrderStatusEditor
{
    Task<int> SetStatusAsync(int orderId, OrderStatus target, string? changedBy);

    Task<int> RefundAsync(int orderId, bool viaPayrexx, string? changedBy);
}

public interface IOrderTickets
{
    Task<int> DeactivateByOrderAsync(int orderId);
}
