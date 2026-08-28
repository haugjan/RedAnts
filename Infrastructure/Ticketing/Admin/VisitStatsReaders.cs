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
    public async Task<SeasonVisitStats> GetAsync(int seasonId, IReadOnlyCollection<int> eventIds)
    {
        var references = await FlexReferencesAsync(seasonId);

        if (eventIds.Count == 0)
            return new SeasonVisitStats(0, 0, new Dictionary<int, int>(), [], [], references);

        var ids = string.Join(",", eventIds);
        using var scope = scopeProvider.CreateScope(autoComplete: true);

        var perEventRows = await scope.Database.FetchAsync<CountRow>($@"
            SELECT EventId AS Grp, COUNT(*) AS Cnt
            FROM TicketEventVisits
            WHERE EventId IN ({ids})
            GROUP BY EventId");
        var perEvent = perEventRows.ToDictionary(r => r.Grp, r => r.Cnt);

        var typeRows = await scope.Database.FetchAsync<CountRow>($@"
            SELECT TicketType AS Grp, COUNT(*) AS Cnt
            FROM TicketEventVisits
            WHERE EventId IN ({ids})
            GROUP BY TicketType");
        var typeBreakdown = typeRows
            .OrderBy(r => r.Grp)
            .Select(r => new VisitTypeSlice(r.Grp, r.Cnt))
            .ToList();

        var freeRows = await scope.Database.FetchAsync<CountRow>($@"
            SELECT f.FreeEntryType AS Grp, COUNT(*) AS Cnt
            FROM TicketEventFreeEntries f
            JOIN TicketEventVisits v ON v.Id = f.VisitId
            WHERE v.EventId IN ({ids})
            GROUP BY f.FreeEntryType");
        var freeBreakdown = freeRows
            .OrderBy(r => r.Grp)
            .Select(r => new FreeEntrySlice(r.Grp, r.Cnt))
            .ToList();

        return new SeasonVisitStats(
            perEvent.Values.Sum(),
            perEvent.Count,
            perEvent,
            typeBreakdown,
            freeBreakdown,
            references);
    }

    private async Task<IReadOnlyList<FlexReferenceRow>> FlexReferencesAsync(int seasonId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<FlexRow>(@"
            SELECT b.Reference AS Reference, b.Category AS Category,
                   COUNT(s.Id) AS Issued,
                   SUM(CASE WHEN s.Redeemed = 1 THEN 1 ELSE 0 END) AS Redeemed
            FROM FlexTicketBundles b
            LEFT JOIN SeasonSingleTickets s ON s.BundleId = b.Id AND s.Status = @0
            WHERE b.SeasonId = @1
            GROUP BY b.Reference, b.Category
            ORDER BY COUNT(s.Id) DESC", (int)TicketStatus.Valid, seasonId);
        return rows
            .Select(r => new FlexReferenceRow(r.Reference, r.Category, r.Issued, r.Redeemed))
            .ToList();
    }

    public sealed class CountRow { public int Grp { get; set; } public int Cnt { get; set; } }
    public sealed class FlexRow { public string Reference { get; set; } = ""; public int Category { get; set; } public int Issued { get; set; } public int Redeemed { get; set; } }
}

public sealed class EventVisitStatsReader(IScopeProvider scopeProvider) : IEventVisitStatsReport
{
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

        var totalVisits = await scope.Database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM TicketEventVisits WHERE EventId = @0", eventId);

        var typeRows = await scope.Database.FetchAsync<CountRow>(@"
            SELECT TicketType AS Grp, COUNT(*) AS Cnt
            FROM TicketEventVisits
            WHERE EventId = @0
            GROUP BY TicketType", eventId);
        var typeBreakdown = typeRows
            .OrderBy(r => r.Grp)
            .Select(r => new VisitTypeSlice(r.Grp, r.Cnt))
            .ToList();

        var freeRows = await scope.Database.FetchAsync<CountRow>(@"
            SELECT f.FreeEntryType AS Grp, COUNT(*) AS Cnt
            FROM TicketEventFreeEntries f
            JOIN TicketEventVisits v ON v.Id = f.VisitId
            WHERE v.EventId = @0
            GROUP BY f.FreeEntryType", eventId);
        var freeBreakdown = freeRows
            .OrderBy(r => r.Grp)
            .Select(r => new FreeEntrySlice(r.Grp, r.Cnt))
            .ToList();

        var logs = await scope.Database.FetchAsync<LogRow>(@"
            SELECT l.VisitId AS VisitId, l.OccurredAt AS OccurredAt, l.Type AS Type
            FROM TicketEventVisitsLogs l
            JOIN TicketEventVisits v ON v.Id = l.VisitId
            WHERE v.EventId = @0
            ORDER BY l.OccurredAt", eventId);

        var (peakInside, peakAtLabel) = ComputePeak(logs);
        var (arrivals, median, average) = ComputeArrivals(logs, kickoffSwiss);

        return new EventVisitStats(
            totalVisits,
            peakInside,
            peakAtLabel,
            median,
            average,
            freeBreakdown.Sum(f => f.Count),
            arrivals,
            typeBreakdown,
            freeBreakdown);
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
            var checkInSwiss = SwissTime.ToSwiss(checkInUtc);
            var before = (int)Math.Round((kickoffSwiss - checkInSwiss).TotalMinutes);
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
        var median = sorted[sorted.Count / 2];
        var average = (int)Math.Round(minutesBefore.Average());
        return (buckets, median, average);
    }

    public sealed class CountRow { public int Grp { get; set; } public int Cnt { get; set; } }
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
