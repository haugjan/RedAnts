using Microsoft.Extensions.Logging;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Ports;

namespace RedAnts.Ticketing.Features.CheckoutWorkflow;

public static class ExpireDraftOrders
{
    public sealed record Command(DateTimeOffset CreatedAfter, DateTimeOffset CreatedBefore);

    public sealed class Handler(IOrders orders, IOrderLog orderLog, CapacityReservation reservation, IPayrexxGateway payrexx,
        OrderFulfillment fulfillment, ILogger<Handler> logger)
    {
        public async Task<int> HandleAsync(Command command)
        {
            var expired = 0;
            foreach (var order in await orders.GetDraftsCreatedBetweenAsync(command.CreatedAfter, command.CreatedBefore))
            {
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
