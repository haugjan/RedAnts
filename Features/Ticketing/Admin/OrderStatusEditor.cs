using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Admin;

public interface IOrderStatusEditor
{
    Task SetStatusAsync(int orderId, OrderStatus target, string? changedBy);

    Task RefundAsync(int orderId, bool viaPayrexx, string? changedBy);
}
