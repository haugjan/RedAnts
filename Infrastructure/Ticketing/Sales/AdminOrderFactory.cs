using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using PaymentMethod = RedAnts.Domain.Ticketing.Sales.PaymentMethod;

namespace RedAnts.Infrastructure.Ticketing.Sales;

public sealed class AdminOrderFactory(IOrders orders, IOrderItems orderItems, IOrderLog orderLog) : IAdminOrderFactory
{
    public async Task<Order> CreateAsync(Buyer buyer, string? email, IReadOnlyList<AdminOrderLine> lines, string createdBy)
    {
        var billing = BillingAddress.FromPersistence((int)buyer.Type, buyer.FirstName ?? "", buyer.LastName ?? "",
            buyer.Company, "", null, "", "", "Schweiz", email ?? "", null);
        var total = lines.Sum(l => decimal.Round(l.UnitPrice * l.Quantity, 2));
        var number = await orders.NextOrderNumberAsync();
        var order = Order.Create(number, billing, total, 0m, PaymentMethod.Manual, sellerUid: null);
        order.MarkPaid();
        var saved = await orders.SaveAsync(order);
        await orderItems.SaveAsync(saved.Id, lines
            .Select(l => OrderItem.Create(saved.Id, l.Kind, l.RefId, l.Category, l.Label, l.Quantity, l.UnitPrice, l.ArticleGuid))
            .ToList());
        await orderLog.AppendAsync(saved.Id, OrderStatus.Draft, createdBy, "Im Backoffice erstellt");
        await orderLog.AppendAsync(saved.Id, OrderStatus.Paid, createdBy, "Backoffice");
        return saved;
    }
}
