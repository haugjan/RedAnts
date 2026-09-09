using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Orders;

namespace RedAnts.Ticketing.Features.Checkout;

public static class GetCheckoutStatus
{
    public sealed record Query(int OrderId);

    public sealed record Result(bool Found, bool Paid, bool Cancelled);

    public sealed class Handler(IOrders orders, IPayrexxGateway payrexx, ILogger<Handler> logger)
    {
        public async Task<Result> HandleAsync(Query query)
        {
            var order = await orders.GetByIdAsync(query.OrderId);
            if (order is null) return new Result(false, false, false);

            var paid = order.Status == OrderStatus.Paid;
            var cancelled = order.Status is OrderStatus.Cancelled or OrderStatus.Refunded;
            if (paid || cancelled || order.Status != OrderStatus.Draft || !payrexx.Enabled || string.IsNullOrEmpty(order.PayrexxGatewayId))
                return new Result(true, paid, cancelled);

            try
            {
                var status = await payrexx.GetGatewayStatusAsync(order.PayrexxGatewayId);
                if (status == PayrexxStatus.Confirmed) paid = true;
                else if (status is PayrexxStatus.Cancelled or PayrexxStatus.Declined) cancelled = true;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Payrexx status check failed for order {Order}.", order.OrderNumber);
            }
            return new Result(true, paid, cancelled);
        }
    }
}
