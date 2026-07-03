using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Admin;

/// <summary>One season pass (Saisonkarte) in the admin list for a season, with the details an admin
/// cares about. A pass is not personalised (unlike a member card); the person shown is the buyer from
/// the linked order. <see cref="EventVisits"/> is the number of distinct events it was admitted to.
/// <see cref="Category"/> and <see cref="Status"/> are the raw enum values so the edit overlay can
/// pre-select them; display labels come from <c>Category.DisplayName()</c> and the tab's status map.</summary>
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

/// <summary>Read side for the Saisonkarten admin tab. Reads the season passes and their buyer/order and
/// visit counts straight from the Sales tables, independent of the ticket repositories.</summary>
public interface ISeasonPassAdminReport
{
    Task<IReadOnlyList<SeasonPassListItem>> GetBySeasonAsync(int seasonId);
}
