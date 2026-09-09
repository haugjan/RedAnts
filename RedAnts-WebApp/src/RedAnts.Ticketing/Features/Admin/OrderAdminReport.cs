using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Admin;

public sealed record OrderListItem(
    int OrderId,
    string OrderNumber,
    DateTimeOffset CreatedAt,
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
    string SeasonPassSummary,
    int FlexTicketCount,
    string FlexTicketSummary,
    PaymentSource? PaymentSource,
    decimal RefundedAmount);

public interface IOrderAdminReport
{
    Task<IReadOnlyList<OrderListItem>> GetBySeasonAsync(int seasonId, IReadOnlyCollection<int> eventIds);
}
