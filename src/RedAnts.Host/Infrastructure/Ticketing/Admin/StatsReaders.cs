using Microsoft.Extensions.DependencyInjection;
using NPoco;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Admin;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Admin;

public sealed class VisitorStatsReader(IScopeProvider scopeProvider) : IVisitorStatsReport
{
    public async Task<VisitorOverview> GetAsync(DateTime fromUtc, DateTime toExclusiveUtc)
    {
        if (toExclusiveUtc <= fromUtc) toExclusiveUtc = fromUtc.AddDays(1);
        var bucket = (toExclusiveUtc - fromUtc).TotalDays <= 92 ? StatBucket.Day : StatBucket.Month;
        var keyExpr = bucket == StatBucket.Day
            ? "CAST(OccurredAt AS date)"
            : "DATEFROMPARTS(YEAR(OccurredAt), MONTH(OccurredAt), 1)";

        using var scope = scopeProvider.CreateScope(autoComplete: true);

        var totals = (await scope.Database.FetchAsync<TotalsRow>(@"
            SELECT
                SUM(CASE WHEN IsBot = 0 THEN 1 ELSE 0 END) AS Views,
                COUNT(DISTINCT CASE WHEN IsBot = 0 THEN VisitorHash END) AS Visitors,
                SUM(CASE WHEN IsBot = 1 THEN 1 ELSE 0 END) AS Bots
            FROM PageViews WHERE OccurredAt >= @0 AND OccurredAt < @1", fromUtc, toExclusiveUtc))
            .FirstOrDefault() ?? new TotalsRow();

        var bucketRows = await scope.Database.FetchAsync<DayRow>($@"
            SELECT {keyExpr} AS Day, COUNT(*) AS Views, COUNT(DISTINCT VisitorHash) AS Visitors
            FROM PageViews
            WHERE IsBot = 0 AND OccurredAt >= @0 AND OccurredAt < @1
            GROUP BY {keyExpr}", fromUtc, toExclusiveUtc);

        var byKey = bucketRows.ToDictionary(r => DateOnly.FromDateTime(r.Day), r => r);

        var series = new List<VisitorBucket>();
        var lastDay = DateOnly.FromDateTime(toExclusiveUtc.AddDays(-1).Date);
        if (bucket == StatBucket.Day)
        {
            for (var d = DateOnly.FromDateTime(fromUtc.Date); d <= lastDay; d = d.AddDays(1))
                series.Add(byKey.TryGetValue(d, out var row)
                    ? new VisitorBucket(d, row.Views, row.Visitors)
                    : new VisitorBucket(d, 0, 0));
        }
        else
        {
            var m = new DateOnly(fromUtc.Year, fromUtc.Month, 1);
            var lastMonth = new DateOnly(lastDay.Year, lastDay.Month, 1);
            for (; m <= lastMonth; m = m.AddMonths(1))
                series.Add(byKey.TryGetValue(m, out var row)
                    ? new VisitorBucket(m, row.Views, row.Visitors)
                    : new VisitorBucket(m, 0, 0));
        }

        var pages = (await scope.Database.FetchAsync<PageRow>(@"
            SELECT TOP 15 Path, COUNT(*) AS Views, COUNT(DISTINCT VisitorHash) AS Visitors
            FROM PageViews
            WHERE IsBot = 0 AND OccurredAt >= @0 AND OccurredAt < @1
            GROUP BY Path
            ORDER BY Views DESC", fromUtc, toExclusiveUtc))
            .Select(p => new VisitorPage(p.Path, p.Views, p.Visitors))
            .ToList();

        return new VisitorOverview(totals.Views, totals.Visitors, totals.Bots, bucket, series, pages);
    }

    public sealed class TotalsRow { public int Views { get; set; } public int Visitors { get; set; } public int Bots { get; set; } }
    public sealed class DayRow { public DateTime Day { get; set; } public int Views { get; set; } public int Visitors { get; set; } }
    public sealed class PageRow { public string Path { get; set; } = ""; public int Views { get; set; } public int Visitors { get; set; } }
}

public sealed class SalesStatsReader(IScopeProvider scopeProvider) : ISalesStatsReport
{
    public async Task<SalesStats> GetSeasonAsync(int seasonId, IReadOnlyCollection<int> eventIds)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);

        var eventAgg = await scope.Database.FetchAsync<EventAggRow>(@"
            SELECT e.EventId AS EventId, COUNT(*) AS Cnt, SUM(e.Price) AS Revenue
            FROM EventTickets e JOIN Orders o ON o.Id = e.OrderId
            WHERE e.Status = @0 AND o.Status = @1
            GROUP BY e.EventId", (int)TicketStatus.Valid, (int)OrderStatus.Paid);

        var eventIdSet = new HashSet<int>(eventIds);
        var perEvent = eventAgg
            .Where(r => eventIdSet.Contains(r.EventId))
            .ToDictionary(r => r.EventId, r => new SalesEventCounts(r.Cnt, r.Revenue ?? 0m));

        var flex = (await scope.Database.FetchAsync<CntRevRow>(@"
            SELECT COUNT(*) AS Cnt, SUM(s.Price) AS Revenue
            FROM SeasonSingleTickets s JOIN Orders o ON o.Id = s.OrderId
            WHERE s.Status = @0 AND o.Status = @1 AND s.SeasonId = @2",
            (int)TicketStatus.Valid, (int)OrderStatus.Paid, seasonId)).FirstOrDefault() ?? new CntRevRow();

        var pass = (await scope.Database.FetchAsync<CntRevRow>(@"
            SELECT COUNT(*) AS Cnt, SUM(p.Price) AS Revenue
            FROM SeasonPasses p JOIN Orders o ON o.Id = p.OrderId
            WHERE p.Status = @0 AND o.Status = @1 AND p.SeasonId = @2",
            (int)TicketStatus.Valid, (int)OrderStatus.Paid, seasonId)).FirstOrDefault() ?? new CntRevRow();

        var memberCount = await scope.Database.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*)
            FROM MembershipCards m LEFT JOIN Orders o ON o.Id = m.OrderId
            WHERE m.Status = @0 AND m.SeasonId = @1 AND (m.OrderId IS NULL OR o.Status = @2)",
            (int)TicketStatus.Valid, seasonId, (int)OrderStatus.Paid);

        return new SalesStats
        {
            EventTicketCount = perEvent.Values.Sum(v => v.TicketsSold),
            EventTicketRevenue = perEvent.Values.Sum(v => v.Revenue),
            FlexCount = flex.Cnt,
            FlexRevenue = flex.Revenue ?? 0m,
            PassCount = pass.Cnt,
            PassRevenue = pass.Revenue ?? 0m,
            MemberCardCount = memberCount,
            PerEvent = perEvent
        };
    }

    public sealed class EventAggRow { public int EventId { get; set; } public int Cnt { get; set; } public decimal? Revenue { get; set; } }
    public sealed class CntRevRow { public int Cnt { get; set; } public decimal? Revenue { get; set; } }
}

public sealed class AdmissionStatsReader(IScopeProvider scopeProvider) : IAdmissionStatsReport
{
    public async Task<IReadOnlyDictionary<int, AdmissionCounts>> GetByEventsAsync(IReadOnlyCollection<int> eventIds)
    {
        if (eventIds.Count == 0) return new Dictionary<int, AdmissionCounts>();

        using var scope = scopeProvider.CreateScope(autoComplete: true);

        var visits = await scope.Database.FetchAsync<VisitAggRow>(@"
            SELECT EventId, COUNT(*) AS Visits, SUM(CASE WHEN IsInside = 1 THEN 1 ELSE 0 END) AS Inside
            FROM TicketEventVisits
            GROUP BY EventId");

        var free = await scope.Database.FetchAsync<FreeAggRow>(@"
            SELECT v.EventId AS EventId, COUNT(*) AS FreeEntries
            FROM TicketEventFreeEntries f JOIN TicketEventVisits v ON v.Id = f.VisitId
            GROUP BY v.EventId");

        var eventIdSet = new HashSet<int>(eventIds);
        var visitById = visits.Where(v => eventIdSet.Contains(v.EventId)).ToDictionary(v => v.EventId);
        var freeById = free.Where(f => eventIdSet.Contains(f.EventId)).ToDictionary(f => f.EventId, f => f.FreeEntries);

        var result = new Dictionary<int, AdmissionCounts>();
        foreach (var id in eventIdSet)
        {
            var v = visitById.GetValueOrDefault(id);
            var freeCount = freeById.GetValueOrDefault(id);
            result[id] = new AdmissionCounts(v?.Visits ?? 0, v?.Inside ?? 0, freeCount);
        }
        return result;
    }

    public sealed class VisitAggRow { public int EventId { get; set; } public int Visits { get; set; } public int Inside { get; set; } }
    public sealed class FreeAggRow { public int EventId { get; set; } public int FreeEntries { get; set; } }
}

public sealed class StatsReadersComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddScoped<IVisitorStatsReport, VisitorStatsReader>();
        builder.Services.AddScoped<ISalesStatsReport, SalesStatsReader>();
        builder.Services.AddScoped<IAdmissionStatsReport, AdmissionStatsReader>();
    }
}
