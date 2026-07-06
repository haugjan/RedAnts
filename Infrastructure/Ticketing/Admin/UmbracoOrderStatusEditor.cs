using Microsoft.Extensions.DependencyInjection;
using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Admin;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace RedAnts.Infrastructure.Ticketing.Admin;

public sealed class UmbracoOrderStatusEditor(IOrders orders, IOrderLog log) : IOrderStatusEditor
{
    public async Task SetStatusAsync(int orderId, OrderStatus target, string? changedBy)
    {
        var order = await orders.GetByIdAsync(orderId)
            ?? throw new DomainException("Bestellung wurde nicht gefunden.");
        if (order.Status == target) return;

        switch (target)
        {
            case OrderStatus.Paid: order.MarkPaid(); break;
            case OrderStatus.Cancelled: order.Cancel(); break;
            case OrderStatus.Refunded: order.Refund(); break;
            default: throw new DomainException("Dieser Bezahlstatus kann nicht gesetzt werden.");
        }

        await orders.SaveAsync(order);
        await log.AppendAsync(orderId, target, changedBy, "Admin-Änderung");
    }
}

public sealed class OrderStatusEditorComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IOrderStatusEditor, UmbracoOrderStatusEditor>();
}
