namespace RedAnts.Features.Ticketing.Admin;

/// <summary>One season pass (Saisonkarte) in the admin list for a season, with the details an admin
/// cares about. A pass is not personalised (unlike a member card); the person shown is the buyer from
/// the linked order. <see cref="EventVisits"/> is the number of distinct events it was admitted to.</summary>
public sealed record SeasonPassListItem(
    Guid Uuid,
    string CategoryName,
    decimal Price,
    string Status,
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
