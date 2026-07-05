using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Admin;

public sealed record OrderListItem(
    string OrderNumber,
    DateTime CreatedAt,
    OrderStatus Status,
    decimal TotalGross,
    BuyerType BuyerType,
    string BuyerName,
    string Street,
    string? AddressLine2,
    string PostalCode,
    string City,
    string Country,
    string Email,
    int EventTicketCount,
    string EventTicketSummary,
    int SeasonPassCount,
    string SeasonPassSummary);

public interface IOrderAdminReport
{
    Task<IReadOnlyList<OrderListItem>> GetBySeasonAsync(int seasonId, IReadOnlyCollection<int> eventIds);
}
