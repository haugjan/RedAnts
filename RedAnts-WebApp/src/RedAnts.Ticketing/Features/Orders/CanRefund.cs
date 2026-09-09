using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Checkout;

namespace RedAnts.Ticketing.Features.Orders;

public static class CanRefund
{
    public sealed record Check(int OrderId, decimal? Amount = null, bool ViaPayrexx = false);

    public sealed class Handler(IOrderRepository orders, IOrderRefunds refunds, IPayrexxGateway payrexx)
    {
        public async Task<CheckResult> HandleAsync(Check check)
        {
            var order = await orders.GetByIdAsync(check.OrderId);
            if (order is null) return CheckResult.Deny(new RefundDenied.OrderUnknown());

            var summary = await refunds.GetSummaryAsync(order.Id);
            return order.RefundBlocker(summary.Remaining, check.Amount, check.ViaPayrexx, payrexx.Enabled);
        }
    }
}
