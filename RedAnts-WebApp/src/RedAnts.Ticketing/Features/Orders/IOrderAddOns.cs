using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Orders;

public sealed record OrderAddOnLine(
    int SeasonId, string SeasonName, TicketCategory Category, string CategoryName,
    string Label, decimal Price, int Quantity, int? TierId = null);

public interface IOrderAddOns
{
    Task SaveAsync(int orderId, IReadOnlyList<OrderAddOnLine> lines);
    Task SetDeliveredAsync(int orderAddOnId, bool delivered);
}
