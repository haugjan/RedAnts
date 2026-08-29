namespace RedAnts.Features.Ticketing.Admin;

public sealed record EventAdmissionCounts(
    int SoldSingleTickets,
    int RedeemedEventTickets,
    int RedeemedSeasonSingleTickets,
    int RedeemedSeasonPasses,
    int RedeemedMemberCards,
    int RedeemedFreeEntries,
    int SeasonPassHolders = 0,
    int MemberHolders = 0,
    int ConvertedSeasonPasses = 0,
    int ConvertedMemberCards = 0,
    int ConvertedFlex = 0,
    int ConvertedPaid = 0,
    decimal ConvertedRevenue = 0m)
{
    public static readonly EventAdmissionCounts Empty = new(0, 0, 0, 0, 0, 0);

    public int TotalRedeemed =>
        RedeemedEventTickets + RedeemedSeasonSingleTickets + RedeemedSeasonPasses
        + RedeemedMemberCards + RedeemedFreeEntries;

    public int ExpectedAdmissions =>
        SoldSingleTickets + SeasonPassHolders + MemberHolders + RedeemedFreeEntries;

    public int TotalConversions => ConvertedSeasonPasses + ConvertedMemberCards + ConvertedFlex;
}

public interface IEventAdmissionReport
{
    Task<IReadOnlyDictionary<int, EventAdmissionCounts>> GetCountsByEventAsync();
}
