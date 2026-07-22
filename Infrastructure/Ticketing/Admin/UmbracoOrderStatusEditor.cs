using Microsoft.Extensions.DependencyInjection;
using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Admin;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace RedAnts.Infrastructure.Ticketing.Admin;

public sealed class UmbracoOrderStatusEditor(IOrders orders, IOrderLog log, IPayrexxGateway payrexx, IOrderTickets orderTickets) : IOrderStatusEditor
{
    public async Task<int> SetStatusAsync(int orderId, OrderStatus target, string? changedBy)
    {
        var order = await orders.GetByIdAsync(orderId)
            ?? throw new DomainException("Bestellung wurde nicht gefunden.");
        if (order.Status == target) return 0;

        switch (target)
        {
            case OrderStatus.Paid: order.MarkPaid(); break;
            case OrderStatus.Draft: order.MarkUnpaid(); break;
            case OrderStatus.Cancelled: order.Cancel(); break;
            case OrderStatus.Refunded: order.Refund(); break;
            default: throw new DomainException("Dieser Bezahlstatus kann nicht gesetzt werden.");
        }

        await orders.SaveAsync(order);

        var deactivated = target is OrderStatus.Cancelled or OrderStatus.Refunded
            ? await orderTickets.DeactivateByOrderAsync(orderId)
            : 0;
        await log.AppendAsync(orderId, target, changedBy, NoteWithTickets("Admin-Änderung", deactivated));
        return deactivated;
    }

    public async Task<int> RefundAsync(int orderId, bool viaPayrexx, string? changedBy)
    {
        var order = await orders.GetByIdAsync(orderId)
            ?? throw new DomainException("Bestellung wurde nicht gefunden.");
        if (order.Status == OrderStatus.Refunded) return 0;
        if (order.Status != OrderStatus.Paid)
            throw new DomainException("Nur bezahlte Bestellungen können zurückerstattet werden.");

        string note;
        if (viaPayrexx)
        {
            if (string.IsNullOrWhiteSpace(order.PayrexxGatewayId) || !payrexx.Enabled)
                throw new DomainException("Diese Bestellung wurde nicht online über Payrexx bezahlt und kann nicht über Payrexx zurückerstattet werden.");
            var result = await payrexx.RefundGatewayAsync(order.PayrexxGatewayId!);
            if (!result.Success)
                throw new DomainException(string.IsNullOrWhiteSpace(result.Error)
                    ? "Payrexx-Rückerstattung fehlgeschlagen. Bitte im Payrexx-Portal prüfen."
                    : $"Payrexx-Rückerstattung fehlgeschlagen: {result.Error}");
            note = "Rückerstattung über Payrexx";
        }
        else
        {
            note = "Rückerstattung (manuell)";
        }

        order.Refund();
        await orders.SaveAsync(order);

        var deactivated = await orderTickets.DeactivateByOrderAsync(orderId);
        await log.AppendAsync(orderId, OrderStatus.Refunded, changedBy, NoteWithTickets(note, deactivated));
        return deactivated;
    }

    private static string NoteWithTickets(string note, int deactivated) =>
        deactivated > 0 ? $"{note} · {deactivated} Ticket(s) deaktiviert" : note;
}

public sealed class OrderStatusEditorComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IOrderStatusEditor, UmbracoOrderStatusEditor>();
}
