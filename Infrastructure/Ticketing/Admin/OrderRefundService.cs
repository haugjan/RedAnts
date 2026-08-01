using Microsoft.Extensions.DependencyInjection;
using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Admin;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace RedAnts.Infrastructure.Ticketing.Admin;

public sealed class OrderRefundService(
    IOrders orders, IOrderRefunds refunds, IPayrexxGateway payrexx, IOrderTickets orderTickets) : IOrderRefundService
{
    public Task<RefundSummary> GetSummaryAsync(int orderId) => refunds.GetSummaryAsync(orderId);

    public Task<IReadOnlyList<OrderRefund>> GetByOrderAsync(int orderId) => refunds.GetByOrderAsync(orderId);

    public async Task<RefundOutcome> RefundAsync(RefundInput input)
    {
        var order = await orders.GetByIdAsync(input.OrderId)
            ?? throw new DomainException("Bestellung wurde nicht gefunden.");
        if (order.Status is not (OrderStatus.Paid or OrderStatus.PartiallyRefunded))
            throw new DomainException("Nur bezahlte Bestellungen können zurückerstattet werden.");
        if (input.Amount <= 0) throw new DomainException("Betrag muss grösser als 0 sein.");

        string refundNumber;
        if (input.ViaPayrexx)
        {
            if (string.IsNullOrWhiteSpace(order.PayrexxGatewayId) || !payrexx.Enabled)
                throw new DomainException("Diese Bestellung wurde nicht online über Payrexx bezahlt und kann nicht über Payrexx zurückerstattet werden.");

            var reserved = await refunds.CreateAsync(input.OrderId, input.Amount, RefundMethod.Payrexx,
                RefundStatus.Pending, input.Reference, input.Reason, input.ChangedBy);
            refundNumber = reserved.RefundNumber;

            var cents = (int)decimal.Round(input.Amount * 100m, 0);
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

            await refunds.ConfirmAsync(reserved.Id, result.RefundId, input.ChangedBy);
        }
        else
        {
            var created = await refunds.CreateAsync(input.OrderId, input.Amount, input.Method,
                RefundStatus.Confirmed, input.Reference, input.Reason, input.ChangedBy);
            refundNumber = created.RefundNumber;
        }

        var deactivated = input.DeactivateTickets ? await orderTickets.DeactivateByOrderAsync(input.OrderId) : 0;

        var summary = await refunds.GetSummaryAsync(input.OrderId);
        var updated = await orders.GetByIdAsync(input.OrderId);
        return new RefundOutcome(refundNumber, summary.RefundedConfirmed, summary.Remaining,
            updated?.Status ?? order.Status, deactivated);
    }
}

public sealed class OrderRefundServiceComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IOrderRefundService, OrderRefundService>();
}
