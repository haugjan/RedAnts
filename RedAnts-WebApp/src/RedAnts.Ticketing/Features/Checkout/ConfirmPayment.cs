using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Orders;

namespace RedAnts.Ticketing.Features.Checkout;

public static class ConfirmPayment
{
    public sealed record Command(int OrderId);

    public sealed record Result(bool Found, bool Paid, bool Cancelled);

    public sealed class Handler(IOrderRepository orders, IPayrexxGateway payrexx, OrderFulfillment fulfillment, CapacityReservation reservation,
        IOrderLog orderLog, ILogger<Handler> logger)
    {
        public async Task<Result> HandleAsync(Command command)
        {
            var order = await orders.GetByIdAsync(command.OrderId);
            if (order is null) return new Result(false, false, false);
            if (order.Status == OrderStatus.Paid) return new Result(true, true, false);
            if (order.Status is OrderStatus.Cancelled or OrderStatus.Refunded)
            {
                await WarnWhenPaidAfterCancellationAsync(order);
                return new Result(true, false, true);
            }
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

        private async Task WarnWhenPaidAfterCancellationAsync(Order order)
        {
            if (order.Status != OrderStatus.Cancelled || !payrexx.Enabled || string.IsNullOrEmpty(order.PayrexxGatewayId)) return;
            try
            {
                if (await payrexx.GetGatewayStatusAsync(order.PayrexxGatewayId) == PayrexxStatus.Confirmed)
                    logger.LogError("Payrexx confirmed the payment for order {Order} after it was cancelled; refund it manually.", order.OrderNumber);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Payrexx status check for cancelled order {Order} failed.", order.OrderNumber);
            }
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
