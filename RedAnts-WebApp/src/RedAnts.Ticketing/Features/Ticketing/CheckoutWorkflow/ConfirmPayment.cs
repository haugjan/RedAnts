using Microsoft.Extensions.Logging;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.CheckoutWorkflow;

public static class ConfirmPayment
{
    public sealed record Command(int OrderId);

    public sealed record Result(bool Found, bool Paid, bool Cancelled);

    public sealed class Handler(IOrders orders, IPayrexxGateway payrexx, OrderFulfillment fulfillment, CapacityReservation reservation,
        IOrderLog orderLog, ILogger<Handler> logger)
    {
        public async Task<Result> HandleAsync(Command command)
        {
            var order = await orders.GetByIdAsync(command.OrderId);
            if (order is null) return new Result(false, false, false);
            if (order.Status == OrderStatus.Paid) return new Result(true, true, false);
            if (order.Status is OrderStatus.Cancelled or OrderStatus.Refunded) return new Result(true, false, true);
            if (order.Status != OrderStatus.Draft || !payrexx.Enabled || string.IsNullOrEmpty(order.PayrexxGatewayId))
                return new Result(true, false, false);

            try
            {
                return await payrexx.GetGatewayStatusAsync(order.PayrexxGatewayId) switch
                {
                    PayrexxStatus.Confirmed => await FulfillAsync(order),
                    PayrexxStatus.Cancelled or PayrexxStatus.Declined => await CancelAsync(order),
                    _ => new Result(true, false, false)
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Payrexx confirmation failed for order {Order}.", order.OrderNumber);
                return new Result(true, false, false);
            }
        }

        private async Task<Result> FulfillAsync(Order order)
        {
            await fulfillment.FulfillAsync(order.Id);
            return new Result(true, true, false);
        }

        private async Task<Result> CancelAsync(Order order)
        {
            if (await orders.TryCancelDraftAsync(order.Id))
            {
                if (OrderSnapshot.Parse(order.FulfillmentPayload) is { } snapshot)
                    await reservation.ReleaseAsync(snapshot);
                await orderLog.AppendAsync(order.Id, OrderStatus.Cancelled, "Payrexx", "Zahlung abgebrochen");
            }
            return new Result(true, false, true);
        }
    }
}
