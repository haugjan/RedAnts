namespace RedAnts.Features.Ticketing.Admin;

/// <summary>One issued Flexticket of a bundle, with just what the CSV export needs: the ticket Uuid
/// (its short code is derived from it) and the season it admits to (the token's scope id).</summary>
public sealed record FlexBundleTicket(Guid Uuid, int SeasonId);

/// <summary>Reads the issued Flextickets (season single tickets) that belong to one bundle, for the
/// bundle CSV export in the Flextickets admin.</summary>
public interface IFlexBundleTickets
{
    Task<IReadOnlyList<FlexBundleTicket>> GetByBundleAsync(int bundleId);
}
