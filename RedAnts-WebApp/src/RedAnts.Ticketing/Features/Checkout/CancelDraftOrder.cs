using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Orders;

namespace RedAnts.Ticketing.Features.Checkout;

public static class CancelDraftOrder
{
    public sealed record Command(int OrderId, string Reason);

    public sealed class Handler(IOrders orders, IOrderLog orderLog, CapacityReservation reservation)
    {
        public async Task<bool> HandleAsync(Command command)
        {
            var order = await orders.GetByIdAsync(command.OrderId);
            if (order is null || !await orders.TryCancelDraftAsync(order.Id)) return false;

            if (OrderSnapshot.Parse(order.FulfillmentPayload) is { } snapshot)
                await reservation.ReleaseAsync(snapshot);
            await orderLog.AppendAsync(order.Id, OrderStatus.Cancelled, "System", command.Reason);
            return true;
        }
    }
}
