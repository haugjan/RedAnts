using Microsoft.Extensions.DependencyInjection;
using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Admin;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace RedAnts.Infrastructure.Ticketing.Admin;

public sealed class UmbracoOrderStatusEditor(IOrders orders, IOrderLog log, IPayrexxGateway payrexx) : IOrderStatusEditor
{
    public async Task SetStatusAsync(int orderId, OrderStatus target, string? changedBy)
    {
        var order = await orders.GetByIdAsync(orderId)
            ?? throw new DomainException("Bestellung wurde nicht gefunden.");
        if (order.Status == target) return;

        switch (target)
        {
            case OrderStatus.Paid: order.MarkPaid(); break;
            case OrderStatus.Draft: order.MarkUnpaid(); break;
            case OrderStatus.Cancelled: order.Cancel(); break;
            case OrderStatus.Refunded: order.Refund(); break;
            default: throw new DomainException("Dieser Bezahlstatus kann nicht gesetzt werden.");
        }

        await orders.SaveAsync(order);
        await log.AppendAsync(orderId, target, changedBy, "Admin-Änderung");
    }

    public async Task RefundAsync(int orderId, bool viaPayrexx, string? changedBy)
    {
        var order = await orders.GetByIdAsync(orderId)
            ?? throw new DomainException("Bestellung wurde nicht gefunden.");
        if (order.Status == OrderStatus.Refunded) return;
        if (order.Status != OrderStatus.Paid)
            throw new DomainException("Nur bezahlte Bestellungen können zurückerstattet werden.");

        string note;
        if (viaPayrexx)
        {
            if (string.IsNullOrWhiteSpace(order.PayrexxGatewayId) || !payrexx.Enabled)
                throw new DomainException("Diese Bestellung wurde nicht online über Payrexx bezahlt und kann nicht über Payrexx zurückerstattet werden.");
            var refunded = await payrexx.RefundGatewayAsync(order.PayrexxGatewayId!);
            if (!refunded)
                throw new DomainException("Payrexx-Rückerstattung fehlgeschlagen. Bitte im Payrexx-Portal prüfen.");
            note = "Rückerstattung über Payrexx";
        }
        else
        {
            note = "Rückerstattung (manuell)";
        }

        order.Refund();
        await orders.SaveAsync(order);
        await log.AppendAsync(orderId, OrderStatus.Refunded, changedBy, note);
    }
}

public sealed class OrderStatusEditorComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IOrderStatusEditor, UmbracoOrderStatusEditor>();
}
