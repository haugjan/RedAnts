using System.Collections.Concurrent;
using NPoco;
using RedAnts.Show.Domain;
using RedAnts.Show.Infrastructure;

namespace RedAnts.Show.Features.Agents.Infrastructure;

public sealed class AgentReports(ShowDatabase database) : IAgentReports
{
    private readonly ConcurrentDictionary<(string Hall, string Kind), AgentReport> _latest = new();

    public async Task RecordAsync(string hallId, AgentReport report)
    {
        _latest[(hallId, report.Kind)] = report;
        if (report.Kind == AgentReportKinds.Clock) return;
        await database.RunAsync(async db =>
        {
            var updated = await db.ExecuteAsync(
                "UPDATE [show].[AgentReports] SET [Json] = @0, [At] = @1 WHERE [HallId] = @2 AND [Kind] = @3",
                report.Json, report.At, hallId, report.Kind);
            if (updated == 0)
                await db.InsertAsync(new AgentReportRecord { HallId = hallId, Kind = report.Kind, Json = report.Json, At = report.At });
        });
    }

    public async Task<IReadOnlyList<AgentReport>> LatestAsync(string hallId)
    {
        var cached = _latest.Where(kv => kv.Key.Hall == hallId).Select(kv => kv.Value).ToList();
        if (cached.Count > 0) return cached;
        var rows = await database.RunAsync(db => db.FetchAsync<AgentReportRecord>(
            "SELECT [HallId],[Kind],[Json],[At] FROM [show].[AgentReports] WHERE [HallId] = @0", hallId));
        var reports = rows.Select(r => new AgentReport(r.Kind, r.Json, r.At)).ToList();
        foreach (var report in reports) _latest[(hallId, report.Kind)] = report;
        return reports;
    }
}
