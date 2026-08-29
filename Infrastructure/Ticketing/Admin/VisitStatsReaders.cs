using Microsoft.Extensions.DependencyInjection;
using NPoco;
using RedAnts.Domain;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Admin;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Admin;

public sealed class SeasonVisitStatsReader(IScopeProvider scopeProvider) : ISeasonVisitStatsReport
{
    private const int Valid = (int)TicketStatus.Valid;
    private const int Paid = (int)OrderStatus.Paid;

    public async Task<SeasonVisitStats> GetAsync(int seasonId, IReadOnlyCollection<int> eventIds, DateTime seasonStartSwiss)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;

        var flex = await FlexFunnelAsync(db, seasonId);
        var passes = await PassUtilizationAsync(db, seasonId, eventIds);
        var members = await MemberUsageAsync(db, seasonId, eventIds);
        var references = await ReferencesAsync(db, seasonId, eventIds);

        if (eventIds.Count == 0)
        {
            return new SeasonVisitStats(0, 0, new Dictionary<int, int>(), [], [], [], [],
                flex, passes, members, references);
        }

        var ids = string.Join(",", eventIds);

        var perEventRows = await db.FetchAsync<CountRow>($@"
            SELECT EventId AS Grp, COUNT(*) AS Cnt
            FROM TicketEventVisits WHERE EventId IN ({ids}) GROUP BY EventId");
        var perEvent = perEventRows.ToDictionary(r => r.Grp, r => r.Cnt);

        var typeRows = await db.FetchAsync<CountRow>($@"
            SELECT TicketType AS Grp, COUNT(*) AS Cnt
            FROM TicketEventVisits WHERE EventId IN ({ids}) GROUP BY TicketType");
        var typeBreakdown = typeRows.OrderBy(r => r.Grp)
            .Select(r => new VisitTypeSlice(r.Grp, r.Cnt)).ToList();

        var freeRows = await db.FetchAsync<CountRow>($@"
            SELECT f.FreeEntryType AS Grp, COUNT(*) AS Cnt
            FROM TicketEventFreeEntries f JOIN TicketEventVisits v ON v.Id = f.VisitId
            WHERE v.EventId IN ({ids}) GROUP BY f.FreeEntryType");
        var freeBreakdown = freeRows.OrderBy(r => r.Grp)
            .Select(r => new FreeEntrySlice(r.Grp, r.Cnt)).ToList();

        var salesByCategory = await SalesByCategoryAsync(db, seasonId, ids);
        var presale = await PresaleAsync(db, seasonId, ids, seasonStartSwiss);

        return new SeasonVisitStats(
            perEvent.Values.Sum(), perEvent.Count, perEvent,
            typeBreakdown, freeBreakdown, salesByCategory, presale,
            flex, passes, members, references);
    }

    private async Task<FlexFunnel> FlexFunnelAsync(IDatabase db, int seasonId)
    {
        var counts = (await db.FetchAsync<FunnelRow>(@"
            SELECT
                SUM(CASE WHEN Status = @0 THEN 1 ELSE 0 END) AS Produced,
                SUM(CASE WHEN Status = @0 AND Redeemed = 1 THEN 1 ELSE 0 END) AS Redeemed,
                SUM(CASE WHEN Status = @0 AND Redeemed = 0 THEN 1 ELSE 0 END) AS Open,
                SUM(CASE WHEN Status = @0 AND OrderId IS NULL THEN 1 ELSE 0 END) AS Gifted
            FROM SeasonSingleTickets WHERE SeasonId = @1", Valid, seasonId)).FirstOrDefault() ?? new FunnelRow();

        var sold = await db.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM SeasonSingleTickets s JOIN Orders o ON o.Id = s.OrderId
            WHERE s.SeasonId = @0 AND s.Status = @1 AND o.Status = @2", seasonId, Valid, Paid);

        return new FlexFunnel(counts.Produced, sold, counts.Gifted, counts.Redeemed, counts.Open);
    }

    private async Task<PassUtilization> PassUtilizationAsync(IDatabase db, int seasonId, IReadOnlyCollection<int> eventIds)
    {
        var passUuids = await db.FetchAsync<string>(
            "SELECT Uuid FROM SeasonPasses WHERE SeasonId = @0 AND Status = @1", seasonId, Valid);

        var games = new Dictionary<string, int>();
        if (eventIds.Count > 0)
        {
            var ids = string.Join(",", eventIds);
            var rows = await db.FetchAsync<UuidCountRow>($@"
                SELECT TicketUuid AS Uuid, COUNT(DISTINCT EventId) AS Cnt
                FROM TicketEventVisits
                WHERE TicketType = {(int)TicketType.SeasonPass} AND EventId IN ({ids}) AND TicketUuid IS NOT NULL
                GROUP BY TicketUuid");
            foreach (var r in rows) games[r.Uuid] = r.Cnt;
        }

        var buckets = new[] { ("0", 0), ("1–3", 0), ("4–7", 0), ("8+", 0) }
            .Select(b => new UtilizationBucket(b.Item1, 0)).ToArray();
        var total = 0;
        var phantom = 0;
        foreach (var uuid in passUuids)
        {
            var g = uuid is null ? 0 : games.GetValueOrDefault(uuid);
            total += g;
            var i = g == 0 ? 0 : g <= 3 ? 1 : g <= 7 ? 2 : 3;
            buckets[i] = buckets[i] with { Count = buckets[i].Count + 1 };
            if (g == 0) phantom++;
        }

        var avg = passUuids.Count == 0 ? 0 : (double)total / passUuids.Count;
        return new PassUtilization(passUuids.Count, avg, phantom, buckets);
    }

    private async Task<MemberUsage> MemberUsageAsync(IDatabase db, int seasonId, IReadOnlyCollection<int> eventIds)
    {
        var total = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM MembershipCards WHERE SeasonId = @0 AND Status = @1", seasonId, Valid);

        if (eventIds.Count == 0 || total == 0) return new MemberUsage(total, 0);

        var ids = string.Join(",", eventIds);
        var active = await db.ExecuteScalarAsync<int>($@"
            SELECT COUNT(DISTINCT v.TicketUuid)
            FROM TicketEventVisits v
            WHERE v.TicketType = {(int)TicketType.MemberCard} AND v.EventId IN ({ids})
              AND v.TicketUuid IN (SELECT Uuid FROM MembershipCards WHERE SeasonId = @0 AND Status = @1)",
            seasonId, Valid);

        return new MemberUsage(total, active);
    }

    private async Task<IReadOnlyList<CategorySlice>> SalesByCategoryAsync(IDatabase db, int seasonId, string ids)
    {
        var rows = await db.FetchAsync<CountRow>($@"
            SELECT Category AS Grp, COUNT(*) AS Cnt FROM (
                SELECT e.Category AS Category FROM EventTickets e JOIN Orders o ON o.Id = e.OrderId
                    WHERE e.EventId IN ({ids}) AND e.Status = @0 AND o.Status = @1
                UNION ALL
                SELECT s.Category FROM SeasonSingleTickets s JOIN Orders o ON o.Id = s.OrderId
                    WHERE s.SeasonId = @2 AND s.Status = @0 AND o.Status = @1
                UNION ALL
                SELECT p.Category FROM SeasonPasses p JOIN Orders o ON o.Id = p.OrderId
                    WHERE p.SeasonId = @2 AND p.Status = @0 AND o.Status = @1
            ) x GROUP BY Category", Valid, Paid, seasonId);
        return rows.OrderBy(r => r.Grp).Select(r => new CategorySlice(r.Grp, r.Cnt)).ToList();
    }

    private async Task<IReadOnlyList<PresaleBucket>> PresaleAsync(IDatabase db, int seasonId, string ids, DateTime seasonStartSwiss)
    {
        var dates = await db.FetchAsync<DateRow>($@"
            SELECT e.CreatedAt AS CreatedAt FROM EventTickets e JOIN Orders o ON o.Id = e.OrderId
                WHERE e.EventId IN ({ids}) AND e.Status = @0 AND o.Status = @1
            UNION ALL
            SELECT s.CreatedAt FROM SeasonSingleTickets s JOIN Orders o ON o.Id = s.OrderId
                WHERE s.SeasonId = @2 AND s.Status = @0 AND o.Status = @1
            UNION ALL
            SELECT p.CreatedAt FROM SeasonPasses p JOIN Orders o ON o.Id = p.OrderId
                WHERE p.SeasonId = @2 AND p.Status = @0 AND o.Status = @1", Valid, Paid, seasonId);

        var labels = new[] { ">8 Wo", "8–6", "6–4", "4–2", "2–0", "nach Start" };
        var buckets = labels.Select(l => new PresaleBucket(l, 0)).ToArray();
        var start = seasonStartSwiss.Date;
        foreach (var row in dates)
        {
            var d = (start - SwissTime.ToSwiss(row.CreatedAt).Date).Days;
            var i = d < 0 ? 5 : d > 56 ? 0 : d > 42 ? 1 : d > 28 ? 2 : d > 14 ? 3 : 4;
            buckets[i] = buckets[i] with { Count = buckets[i].Count + 1 };
        }
        return buckets;
    }

    private async Task<IReadOnlyList<ReferenceRow>> ReferencesAsync(IDatabase db, int seasonId, IReadOnlyCollection<int> eventIds)
    {
        var flex = await db.FetchAsync<RefRow>(@"
            SELECT b.Reference AS Reference, b.Category AS Category,
                   COUNT(s.Id) AS Issued,
                   SUM(CASE WHEN s.Redeemed = 1 THEN 1 ELSE 0 END) AS Redeemed
            FROM FlexTicketBundles b
            LEFT JOIN SeasonSingleTickets s ON s.BundleId = b.Id AND s.Status = @0
            WHERE b.SeasonId = @1
            GROUP BY b.Reference, b.Category", Valid, seasonId);

        var result = flex
            .Select(r => new ReferenceRow(r.Reference, "Flexticket", r.Category, r.Issued, r.Redeemed))
            .ToList();

        if (eventIds.Count > 0)
        {
            var ids = string.Join(",", eventIds);
            var events = await db.FetchAsync<RefRow>($@"
                SELECT b.Reference AS Reference, b.Category AS Category,
                       COUNT(t.Id) AS Issued,
                       SUM(CASE WHEN t.Redeemed = 1 THEN 1 ELSE 0 END) AS Redeemed
                FROM EventTicketBundles b
                LEFT JOIN EventTickets t ON t.BundleId = b.Id AND t.Status = @0
                WHERE b.EventId IN ({ids})
                GROUP BY b.Reference, b.Category", Valid);
            result.AddRange(events.Select(r =>
                new ReferenceRow(r.Reference, "Spielticket", r.Category, r.Issued, r.Redeemed)));
        }

        return result
            .OrderByDescending(r => r.Issued == 0 ? 0d : (double)r.Redeemed / r.Issued)
            .ThenByDescending(r => r.Issued)
            .ToList();
    }

    public sealed class CountRow { public int Grp { get; set; } public int Cnt { get; set; } }
    public sealed class UuidCountRow { public string Uuid { get; set; } = ""; public int Cnt { get; set; } }
    public sealed class FunnelRow { public int Produced { get; set; } public int Redeemed { get; set; } public int Open { get; set; } public int Gifted { get; set; } }
    public sealed class DateRow { public DateTime CreatedAt { get; set; } }
    public sealed class RefRow { public string Reference { get; set; } = ""; public int Category { get; set; } public int Issued { get; set; } public int Redeemed { get; set; } }
}

public sealed class EventVisitStatsReader(IScopeProvider scopeProvider) : IEventVisitStatsReport
{
    private const int Valid = (int)TicketStatus.Valid;
    private const int Paid = (int)OrderStatus.Paid;

    private static readonly (string Label, int LowMinutes, int HighMinutes)[] ArrivalWindows =
    [
        ("> 60 Min.", 60, int.MaxValue),
        ("60–45", 45, 60),
        ("45–30", 30, 45),
        ("30–15", 15, 30),
        ("15–0", 0, 15),
        ("nach Anpfiff", int.MinValue, 0)
    ];

    public async Task<EventVisitStats> GetAsync(int eventId, DateTime kickoffSwiss)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;

        var totalVisits = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM TicketEventVisits WHERE EventId = @0", eventId);

        var typeRows = await db.FetchAsync<CountRow>(@"
            SELECT TicketType AS Grp, COUNT(*) AS Cnt
            FROM TicketEventVisits WHERE EventId = @0 GROUP BY TicketType", eventId);
        var typeBreakdown = typeRows.OrderBy(r => r.Grp)
            .Select(r => new VisitTypeSlice(r.Grp, r.Cnt)).ToList();

        var freeRows = await db.FetchAsync<CountRow>(@"
            SELECT f.FreeEntryType AS Grp, COUNT(*) AS Cnt
            FROM TicketEventFreeEntries f JOIN TicketEventVisits v ON v.Id = f.VisitId
            WHERE v.EventId = @0 GROUP BY f.FreeEntryType", eventId);
        var freeBreakdown = freeRows.OrderBy(r => r.Grp)
            .Select(r => new FreeEntrySlice(r.Grp, r.Cnt)).ToList();

        var sold = await db.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM EventTickets e JOIN Orders o ON o.Id = e.OrderId
            WHERE e.EventId = @0 AND e.Status = @1 AND o.Status = @2", eventId, Valid, Paid);
        var redeemed = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM EventTickets WHERE EventId = @0 AND Status = @1 AND Redeemed = 1", eventId, Valid);
        var noShow = Math.Max(0, sold - redeemed);

        var presaleShare = await PresaleShareAsync(db, eventId, kickoffSwiss);

        var logs = await db.FetchAsync<LogRow>(@"
            SELECT l.VisitId AS VisitId, l.OccurredAt AS OccurredAt, l.Type AS Type
            FROM TicketEventVisitsLogs l JOIN TicketEventVisits v ON v.Id = l.VisitId
            WHERE v.EventId = @0 ORDER BY l.OccurredAt", eventId);

        var (peakInside, peakAtLabel) = ComputePeak(logs);
        var (arrivals, median, average) = ComputeArrivals(logs, kickoffSwiss);

        return new EventVisitStats(
            totalVisits, peakInside, peakAtLabel, median, average,
            freeBreakdown.Sum(f => f.Count),
            sold, redeemed, noShow, presaleShare,
            arrivals, typeBreakdown, freeBreakdown);
    }

    private async Task<int?> PresaleShareAsync(IDatabase db, int eventId, DateTime kickoffSwiss)
    {
        var dates = await db.FetchAsync<DateRow>(@"
            SELECT e.CreatedAt AS CreatedAt FROM EventTickets e JOIN Orders o ON o.Id = e.OrderId
            WHERE e.EventId = @0 AND e.Status = @1 AND o.Status = @2", eventId, Valid, Paid);
        if (dates.Count == 0) return null;
        var eventDay = kickoffSwiss.Date;
        var presale = dates.Count(d => SwissTime.ToSwiss(d.CreatedAt).Date < eventDay);
        return (int)Math.Round(presale * 100.0 / dates.Count);
    }

    private static (int Peak, string? AtLabel) ComputePeak(IReadOnlyList<LogRow> logs)
    {
        var inside = 0;
        var peak = 0;
        DateTime? peakAt = null;
        foreach (var log in logs)
        {
            inside += log.Type == (int)VisitLogType.CheckIn ? 1 : -1;
            if (inside > peak)
            {
                peak = inside;
                peakAt = log.OccurredAt;
            }
        }
        return (peak, peakAt is null ? null : SwissTime.ToSwiss(peakAt.Value).ToString("HH:mm"));
    }

    private static (IReadOnlyList<ArrivalBucket> Buckets, int? Median, int? Average) ComputeArrivals(
        IReadOnlyList<LogRow> logs, DateTime kickoffSwiss)
    {
        var firstCheckIn = logs
            .Where(l => l.Type == (int)VisitLogType.CheckIn)
            .GroupBy(l => l.VisitId)
            .Select(g => g.Min(l => l.OccurredAt))
            .ToList();

        var buckets = ArrivalWindows.Select(w => new ArrivalBucket(w.Label, 0)).ToArray();
        var minutesBefore = new List<int>();

        foreach (var checkInUtc in firstCheckIn)
        {
            var before = (int)Math.Round((kickoffSwiss - SwissTime.ToSwiss(checkInUtc)).TotalMinutes);
            minutesBefore.Add(before);
            for (var i = 0; i < ArrivalWindows.Length; i++)
            {
                var w = ArrivalWindows[i];
                if (before >= w.LowMinutes && before < w.HighMinutes)
                {
                    buckets[i] = buckets[i] with { Count = buckets[i].Count + 1 };
                    break;
                }
            }
        }

        if (minutesBefore.Count == 0) return (buckets, null, null);
        var sorted = minutesBefore.OrderBy(m => m).ToList();
        return (buckets, sorted[sorted.Count / 2], (int)Math.Round(minutesBefore.Average()));
    }

    public sealed class CountRow { public int Grp { get; set; } public int Cnt { get; set; } }
    public sealed class DateRow { public DateTime CreatedAt { get; set; } }
    public sealed class LogRow { public long VisitId { get; set; } public DateTime OccurredAt { get; set; } public int Type { get; set; } }
}

public sealed class VisitStatsReadersComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddScoped<ISeasonVisitStatsReport, SeasonVisitStatsReader>();
        builder.Services.AddScoped<IEventVisitStatsReport, EventVisitStatsReader>();
    }
}
