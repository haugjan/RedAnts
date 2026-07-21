namespace RedAnts.Features.Ticketing.Admin;

public sealed record EventAdmissionCounts(
    int SoldSingleTickets,
    int RedeemedEventTickets,
    int RedeemedSeasonSingleTickets,
    int RedeemedSeasonPasses,
    int RedeemedMemberCards,
    int RedeemedFreeEntries,
    int SeasonPassHolders = 0,
    int MemberHolders = 0)
{
    public static readonly EventAdmissionCounts Empty = new(0, 0, 0, 0, 0, 0);

    public int TotalRedeemed =>
        RedeemedEventTickets + RedeemedSeasonSingleTickets + RedeemedSeasonPasses
        + RedeemedMemberCards + RedeemedFreeEntries;

    public int ExpectedAdmissions =>
        SoldSingleTickets + SeasonPassHolders + MemberHolders + RedeemedFreeEntries;
}

public interface IEventAdmissionReport
{
    Task<IReadOnlyDictionary<int, EventAdmissionCounts>> GetCountsByEventAsync();
}
