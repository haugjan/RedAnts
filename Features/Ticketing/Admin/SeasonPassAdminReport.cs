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
    string? PaymentState);

public interface ISeasonPassAdminReport
{
    Task<IReadOnlyList<SeasonPassListItem>> GetBySeasonAsync(int seasonId);
}
