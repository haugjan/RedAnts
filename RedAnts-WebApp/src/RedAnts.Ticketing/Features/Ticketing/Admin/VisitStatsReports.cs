namespace RedAnts.Features.Ticketing.Admin;

public sealed record VisitTypeSlice(int TicketType, int Count);

public sealed record FreeEntrySlice(int FreeEntryType, int Count);

public sealed record CategorySlice(int Category, int Count);

public sealed record ArrivalBucket(string Label, int Count);

public sealed record UtilizationBucket(string Label, int Count);

public sealed record PresaleBucket(string Label, int Count);

public sealed record FlexFunnel(int Produced, int Sold, int Gifted, int Redeemed, int Open);

public sealed record PassUtilization(
    int PassCount, double AvgGames, int Phantom, IReadOnlyList<UtilizationBucket> Buckets);

public sealed record MemberUsage(int Total, int Active);

public sealed record ReferenceRow(string Reference, string Kind, int Category, int Issued, int Redeemed);

public sealed record SeasonVisitStats(
    int TotalVisits,
    int EventCount,
    IReadOnlyDictionary<int, int> PerEvent,
    IReadOnlyList<VisitTypeSlice> TypeBreakdown,
    IReadOnlyList<FreeEntrySlice> FreeEntryBreakdown,
    IReadOnlyList<CategorySlice> SalesByCategory,
    IReadOnlyList<PresaleBucket> Presale,
    FlexFunnel Flex,
    PassUtilization Passes,
    MemberUsage Members,
    IReadOnlyList<ReferenceRow> References);

public interface ISeasonVisitStatsReport
{
    Task<SeasonVisitStats> GetAsync(int seasonId, IReadOnlyCollection<int> eventIds, DateTime seasonStartSwiss);
}

public sealed record EventVisitStats(
    int TotalVisits,
    int PeakInside,
    string? PeakAtLabel,
    int? MedianArrivalMinutes,
    int? AverageArrivalMinutes,
    int FreeEntries,
    int EventTicketsSold,
    int EventTicketsRedeemed,
    int NoShow,
    int? PresaleSharePercent,
    IReadOnlyList<ArrivalBucket> Arrivals,
    IReadOnlyList<VisitTypeSlice> TypeBreakdown,
    IReadOnlyList<FreeEntrySlice> FreeEntryBreakdown);

public interface IEventVisitStatsReport
{
    Task<EventVisitStats> GetAsync(int eventId, DateTime kickoffSwiss);
}
