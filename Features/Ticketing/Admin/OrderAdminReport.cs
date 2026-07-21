using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Admin;

public sealed record OrderListItem(
    int OrderId,
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
    string SeasonPassSummary,
    int MemberCardCount,
    string MemberCardSummary,
    int FlexTicketCount,
    string FlexTicketSummary,
    PaymentSource? PaymentSource);

public interface IOrderAdminReport
{
    Task<IReadOnlyList<OrderListItem>> GetBySeasonAsync(int seasonId, IReadOnlyCollection<int> eventIds);
}
