namespace RedAnts.Features.Ticketing.Admin;

public sealed record VisitorDay(DateOnly Day, int Views, int Visitors);

public sealed record VisitorPage(string Path, int Views, int Visitors);

public sealed record VisitorOverview(
    int TotalViews,
    int TotalVisitors,
    int BotViews,
    IReadOnlyList<VisitorDay> Days,
    IReadOnlyList<VisitorPage> TopPages);

public interface IVisitorStatsReport
{
    Task<VisitorOverview> GetAsync(int days);
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
