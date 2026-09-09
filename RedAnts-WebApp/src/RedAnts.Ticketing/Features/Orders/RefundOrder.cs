using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Checkout;

namespace RedAnts.Ticketing.Features.Orders;

public sealed record RefundRequest(
    int OrderId,
    decimal Amount,
    RefundMethod Method,
    bool ViaPayrexx,
    string? Reference,
    string? Reason,
    bool DeactivateTickets,
    string? ChangedBy);

public sealed record RefundResult(
    string RefundNumber,
    decimal RefundedTotal,
    decimal Remaining,
    OrderStatus Status,
    int DeactivatedTickets);

public static class RefundOrder
{
    public sealed record Command(RefundRequest Request);

    public sealed class Handler(IOrderRepository orders, IOrderRefunds refunds, IPayrexxGateway payrexx, IOrderTickets orderTickets,
        IUnitOfWork unitOfWork)
    {
        private sealed record Settlement(string RefundNumber, int DeactivatedTickets);

        public async Task<RefundResult> HandleAsync(Command command)
        {
            var request = command.Request;
            var order = await orders.GetByIdAsync(request.OrderId)
                ?? throw new DomainException("Bestellung wurde nicht gefunden.");
            order.RequireRefundable();
            if (request.Amount <= 0) throw new DomainException("Betrag muss grösser als 0 sein.");

            var open = await refunds.GetSummaryAsync(order.Id);
            if (request.Amount > open.Remaining)
                throw new DomainException($"Der Betrag übersteigt den noch offenen Rest von CHF {open.Remaining:0.00}.");

            var settlement = request.ViaPayrexx
                ? await SettleThroughPayrexxAsync(order, request)
                : await unitOfWork.RunAsync(() => SettleManuallyAsync(order, request));

            var summary = await refunds.GetSummaryAsync(order.Id);
            var updated = await orders.GetByIdAsync(order.Id);
            return new RefundResult(settlement.RefundNumber, summary.RefundedConfirmed, summary.Remaining,
                updated?.Status ?? order.Status, settlement.DeactivatedTickets);
        }

        private async Task<Settlement> SettleManuallyAsync(Order order, RefundRequest request)
        {
            var refund = await refunds.CreateAsync(order.Id, request.Amount, request.Method, RefundStatus.Confirmed,
                request.Reference, request.Reason, request.ChangedBy);
            return new Settlement(refund.RefundNumber, await DeactivateTicketsAsync(order, request));
        }

        private async Task<Settlement> SettleThroughPayrexxAsync(Order order, RefundRequest request)
        {
            if (!order.PaidThroughPayrexx || !payrexx.Enabled)
                throw new DomainException("Diese Bestellung wurde nicht online über Payrexx bezahlt und kann nicht über Payrexx zurückerstattet werden.");

            var reserved = await refunds.CreateAsync(order.Id, request.Amount, RefundMethod.Payrexx, RefundStatus.Pending,
                request.Reference, request.Reason, request.ChangedBy);

            var cents = (int)decimal.Round(request.Amount * 100m, 0);
            PayrexxRefundResult result;
            try
            {
                result = await payrexx.RefundGatewayAsync(order.PayrexxGatewayId!, cents);
            }
            catch (Exception ex)
            {
                await refunds.FailAsync(reserved.Id, ex.Message);
                throw new DomainException($"Payrexx-Rückerstattung fehlgeschlagen: {ex.Message}");
            }

            if (!result.Success)
            {
                await refunds.FailAsync(reserved.Id, result.Error);
                throw new DomainException(string.IsNullOrWhiteSpace(result.Error)
                    ? "Payrexx-Rückerstattung fehlgeschlagen. Bitte im Payrexx-Portal prüfen."
                    : $"Payrexx-Rückerstattung fehlgeschlagen: {result.Error}");
            }

            return await unitOfWork.RunAsync(async () =>
            {
                await refunds.ConfirmAsync(reserved.Id, result.RefundId, request.ChangedBy);
                return new Settlement(reserved.RefundNumber, await DeactivateTicketsAsync(order, request));
            });
        }

        private Task<int> DeactivateTicketsAsync(Order order, RefundRequest request) =>
            request.DeactivateTickets ? orderTickets.DeactivateByOrderAsync(order.Id) : Task.FromResult(0);
    }
}
