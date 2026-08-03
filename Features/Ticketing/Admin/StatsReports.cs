namespace RedAnts.Features.Ticketing.Admin;

public enum StatBucket { Day, Month }

public sealed record VisitorBucket(DateOnly Start, int Views, int Visitors);

public sealed record VisitorPage(string Path, int Views, int Visitors);

public sealed record VisitorOverview(
    int TotalViews,
    int TotalVisitors,
    int BotViews,
    StatBucket Bucket,
    IReadOnlyList<VisitorBucket> Series,
    IReadOnlyList<VisitorPage> TopPages);

public interface IVisitorStatsReport
{
    Task<VisitorOverview> GetAsync(DateTime fromUtc, DateTime toExclusiveUtc);
}

public sealed class SalesStats
{
    public int EventTicketCount { get; init; }
    public decimal EventTicketRevenue { get; init; }
    public int FlexCount { get; init; }
    public decimal FlexRevenue { get; init; }
    public int PassCount { get; init; }
    public decimal PassRevenue { get; init; }
    public int MemberCardCount { get; init; }
    public IReadOnlyDictionary<int, SalesEventCounts> PerEvent { get; init; } =
        new Dictionary<int, SalesEventCounts>();
}

public sealed record SalesEventCounts(int TicketsSold, decimal Revenue);

public interface ISalesStatsReport
{
    Task<SalesStats> GetSeasonAsync(int seasonId, IReadOnlyCollection<int> eventIds);
}

public sealed record AdmissionCounts(int Visits, int Inside, int FreeEntries);

public interface IAdmissionStatsReport
{
    Task<IReadOnlyDictionary<int, AdmissionCounts>> GetByEventsAsync(IReadOnlyCollection<int> eventIds);
}
