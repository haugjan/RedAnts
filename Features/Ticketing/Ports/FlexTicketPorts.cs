using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Ports;

/// <summary>A Flexticket bundle together with its issued/redeemed ticket counts, for the bundle-wise
/// admin listing.</summary>
public sealed record FlexTicketBundleView(
    int Id,
    int SeasonId,
    TicketCategory Category,
    string Reference,
    DateTime CreatedAt,
    int TicketCount,
    int RedeemedCount);

/// <summary>Flexticket bundles (batches of season single-admission tickets). Flextickets are always
/// created as a bundle: creating one issues its N season single tickets in the same step.</summary>
public interface IFlexTicketBundles
{
    Task<IReadOnlyList<FlexTicketBundleView>> GetBySeasonAsync(int seasonId);

    /// <summary>True if the reference is already used within the season (references are unique per season).</summary>
    Task<bool> ReferenceExistsAsync(int seasonId, string reference);

    /// <summary>Creates a bundle and issues <paramref name="quantity"/> Flextickets bound to it.</summary>
    Task<FlexTicketBundleView> CreateAsync(int seasonId, TicketCategory category, string reference, int quantity);
}
