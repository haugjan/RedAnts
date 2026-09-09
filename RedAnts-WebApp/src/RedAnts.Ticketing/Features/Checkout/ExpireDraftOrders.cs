using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Orders;

namespace RedAnts.Ticketing.Features.Checkout;

public static class ExpireDraftOrders
{
    public static readonly TimeSpan MaxDraftAge = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan LookBack = TimeSpan.FromDays(2);

    public sealed record Command(DateTimeOffset CreatedAfter, DateTimeOffset CreatedBefore)
    {
        public static Command Due(TimeProvider time)
        {
            var now = SwissTime.TimestampOf(time);
            return new Command(now - LookBack, now - MaxDraftAge);
        }
    }

    public sealed class Handler(IOrderRepository orders, IDraftOrdersReader drafts, IOrderLog orderLog, CapacityReservation reservation,
        IPayrexxGateway payrexx, OrderFulfillment fulfillment, ILogger<Handler> logger)
    {
        public async Task<int> HandleAsync(Command command)
        {
            var expired = 0;
            foreach (var orderId in await drafts.GetIdsCreatedBetweenAsync(command.CreatedAfter, command.CreatedBefore))
            {
                var order = await orders.GetByIdAsync(orderId);
                if (order is null) continue;
                try
                {
                    if (await PaidMeanwhileAsync(order))
                    {
                        await fulfillment.FulfillAsync(order.Id);
                        continue;
                    }
                    if (!await orders.TryCancelDraftAsync(order.Id)) continue;
                    if (OrderSnapshot.Parse(order.FulfillmentPayload) is { } snapshot)
                        await reservation.ReleaseAsync(snapshot);
                    await orderLog.AppendAsync(order.Id, OrderStatus.Cancelled, "System", "Reservation abgelaufen");
                    expired++;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Draft order {Order} could not be expired.", order.OrderNumber);
                }
            }
            return expired;
        }

        private async Task<bool> PaidMeanwhileAsync(Order order)
        {
            if (!payrexx.Enabled || string.IsNullOrEmpty(order.PayrexxGatewayId)) return false;
            return await payrexx.GetGatewayStatusAsync(order.PayrexxGatewayId) == PayrexxStatus.Confirmed;
        }
    }
}
