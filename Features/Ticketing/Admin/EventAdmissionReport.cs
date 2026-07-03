namespace RedAnts.Features.Ticketing.Admin;

/// <summary>Per-event admission counts across the ticket types. "Redeemed" means the ticket was
/// admitted at least once to that event. Einlasskontingent and the new entitlement type are built in
/// separate sessions and are not part of this read model yet (rendered as placeholders in the table).</summary>
public sealed record EventAdmissionCounts(
    int SoldSingleTickets,
    int RedeemedEventTickets,
    int RedeemedSeasonSingleTickets,
    int RedeemedSeasonPasses,
    int RedeemedMemberCards)
{
    public static readonly EventAdmissionCounts Empty = new(0, 0, 0, 0, 0);
}

/// <summary>Read side for the Anlässe admin table. Self-contained: aggregates counts straight from the
/// Sales tables, keyed by event id, independent of the ticket repositories.</summary>
public interface IEventAdmissionReport
{
    Task<IReadOnlyDictionary<int, EventAdmissionCounts>> GetCountsByEventAsync();
}
