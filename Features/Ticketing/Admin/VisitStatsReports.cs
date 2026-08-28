namespace RedAnts.Features.Ticketing.Admin;

public sealed record VisitTypeSlice(int TicketType, int Count);

public sealed record FreeEntrySlice(int FreeEntryType, int Count);

public sealed record EventVisitBar(int EventId, int Visits);

public sealed record FlexReferenceRow(string Reference, int Category, int Issued, int Redeemed);

public sealed record SeasonVisitStats(
    int TotalVisits,
    int EventCount,
    IReadOnlyDictionary<int, int> PerEvent,
    IReadOnlyList<VisitTypeSlice> TypeBreakdown,
    IReadOnlyList<FreeEntrySlice> FreeEntryBreakdown,
    IReadOnlyList<FlexReferenceRow> FlexReferences);

public interface ISeasonVisitStatsReport
{
    Task<SeasonVisitStats> GetAsync(int seasonId, IReadOnlyCollection<int> eventIds);
}

public sealed record ArrivalBucket(string Label, int Count);

public sealed record EventVisitStats(
    int TotalVisits,
    int PeakInside,
    string? PeakAtLabel,
    int? MedianArrivalMinutes,
    int? AverageArrivalMinutes,
    int FreeEntries,
    IReadOnlyList<ArrivalBucket> Arrivals,
    IReadOnlyList<VisitTypeSlice> TypeBreakdown,
    IReadOnlyList<FreeEntrySlice> FreeEntryBreakdown);

public interface IEventVisitStatsReport
{
    Task<EventVisitStats> GetAsync(int eventId, DateTime kickoffSwiss);
}
