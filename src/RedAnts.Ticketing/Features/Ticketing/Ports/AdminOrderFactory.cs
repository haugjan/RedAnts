using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Ports;

public sealed record AdminOrderLine(
    OrderItemKind Kind, int RefId, TicketCategory Category, string Label, int Quantity, decimal UnitPrice, Guid? ArticleGuid = null);

public interface IAdminOrderFactory
{
    Task<Order> CreateAsync(Buyer buyer, string? email, IReadOnlyList<AdminOrderLine> lines, string createdBy,
        PaymentSource paymentSource);
}
