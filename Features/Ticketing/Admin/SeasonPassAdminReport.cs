using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Admin;

public sealed record SeasonPassListItem(
    Guid Uuid,
    TicketCategory Category,
    decimal Price,
    TicketStatus Status,
    DateTime CreatedAt,
    int EventVisits,
    string? BuyerName,
    string? OrderNumber,
    string? PaymentState,
    BuyerType? BuyerType = null,
    string? CreatedByName = null,
    string? Reference = null);

public interface ISeasonPassAdminReport
{
    Task<IReadOnlyList<SeasonPassListItem>> GetBySeasonAsync(int seasonId);
}
