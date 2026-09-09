using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Domain.Sales;
using PaymentMethod = RedAnts.Ticketing.Domain.Sales.PaymentMethod;

namespace RedAnts.Ticketing.Features.Orders;

public sealed record AdminOrderLine(
    OrderItemKind Kind, int RefId, TicketCategory Category, string Label, int Quantity, decimal UnitPrice, Guid? ArticleGuid = null);

public static class CreateAdminOrder
{
    public sealed record Command(Buyer Buyer, string? Email, IReadOnlyList<AdminOrderLine> Lines, string CreatedBy, PaymentSource PaymentSource);

    public sealed class Handler(IOrders orders, IOrderItems orderItems, IOrderLog orderLog)
    {
        public async Task<Order> HandleAsync(Command command)
        {
            var buyer = command.Buyer;
            var billing = BillingAddress.FromPersistence((int)buyer.Type, buyer.FirstName ?? "", buyer.LastName ?? "",
                buyer.Company, "", null, "", "", "Schweiz", command.Email ?? "", null);
            var total = command.Lines.Sum(l => decimal.Round(l.UnitPrice * l.Quantity, 2));
            var number = await orders.NextOrderNumberAsync();
            var order = Order.Create(number, billing, total, 0m, PaymentMethod.Manual, sellerUid: null,
                paymentSource: command.PaymentSource);
            order.MarkPaid();
            var saved = await orders.SaveAsync(order);
            await orderItems.SaveAsync(saved.Id, command.Lines
                .Select(l => OrderItem.Create(saved.Id, l.Kind, l.RefId, l.Category, l.Label, l.Quantity, l.UnitPrice, l.ArticleGuid))
                .ToList());
            await orderLog.AppendAsync(saved.Id, OrderStatus.Draft, command.CreatedBy, "Im Backoffice erstellt");
            await orderLog.AppendAsync(saved.Id, OrderStatus.Paid, command.CreatedBy, "Backoffice");
            return saved;
        }
    }
}
