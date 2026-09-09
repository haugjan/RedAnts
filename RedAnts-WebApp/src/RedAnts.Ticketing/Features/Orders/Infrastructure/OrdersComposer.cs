using Umbraco.Cms.Core.Composing;

namespace RedAnts.Ticketing.Features.Orders.Infrastructure;

public sealed class OrdersComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddScoped<IOrderRepository, OrderRepository>();
        builder.Services.AddScoped<IOrderItems, OrderItemRepository>();
        builder.Services.AddScoped<IOrderLog, OrderLogRepository>();
        builder.Services.AddScoped<IOrderRefunds, OrderRefundRepository>();
        builder.Services.AddScoped<IOrderTickets, OrderTicketDeactivator>();
        builder.Services.AddScoped<IOrderAddOns, OrderAddOnRepository>();
        builder.Services.AddScoped<IDraftOrdersReader, DraftOrdersReader>();
        builder.Services.AddScoped<IOrderListReader, OrderListReader>();
        builder.Services.AddScoped<IOrderDetailReader, OrderDetailReader>();
        builder.Services.AddScoped<IOrderAddOnListReader, OrderAddOnListReader>();
    }
}
