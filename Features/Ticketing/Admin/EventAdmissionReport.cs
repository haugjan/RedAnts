namespace RedAnts.Features.Ticketing.Admin;

public sealed record EventAdmissionCounts(
    int SoldSingleTickets,
    int RedeemedEventTickets,
    int RedeemedSeasonSingleTickets,
    int RedeemedSeasonPasses,
    int RedeemedMemberCards,
    int RedeemedFreeEntries)
{
    public static readonly EventAdmissionCounts Empty = new(0, 0, 0, 0, 0, 0);

    public int TotalRedeemed =>
        RedeemedEventTickets + RedeemedSeasonSingleTickets + RedeemedSeasonPasses
        + RedeemedMemberCards + RedeemedFreeEntries;
}

public interface IEventAdmissionReport
{
    Task<IReadOnlyDictionary<int, EventAdmissionCounts>> GetCountsByEventAsync();
}
