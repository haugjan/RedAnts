using Microsoft.Extensions.Logging;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.CheckoutWorkflow;

public static class ExpireDraftOrders
{
    public sealed record Command(DateTime CreatedBefore);

    public sealed class Handler(IOrders orders, IOrderLog orderLog, CapacityReservation reservation, ILogger<Handler> logger)
    {
        public async Task<int> HandleAsync(Command command)
        {
            var expired = 0;
            foreach (var order in await orders.GetDraftsCreatedBeforeAsync(command.CreatedBefore))
            {
                try
                {
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
    }
}
