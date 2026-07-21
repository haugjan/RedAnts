using Microsoft.Extensions.DependencyInjection;
using RedAnts.Features.Ticketing.Admin;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Admin;

public sealed class HelperScanReportReader(IScopeProvider scopeProvider) : IHelperScanReport
{
    public async Task<IReadOnlyList<HelperScanRow>> GetByEventsAsync(IReadOnlyCollection<int> eventIds)
    {
        if (eventIds.Count == 0) return [];
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var ids = string.Join(",", eventIds);
        var rows = await scope.Database.FetchAsync<Row>($@"
            SELECT v.EventId AS EventId, l.ScannedBy AS Person,
                   SUM(CASE WHEN l.Type = 0 THEN 1 ELSE 0 END) AS CheckIns,
                   SUM(CASE WHEN l.Type = 1 THEN 1 ELSE 0 END) AS CheckOuts
            FROM TicketEventVisitsLogs l
            INNER JOIN TicketEventVisits v ON v.Id = l.VisitId
            WHERE v.EventId IN ({ids}) AND l.ScannedBy IS NOT NULL AND l.ScannedBy <> ''
            GROUP BY v.EventId, l.ScannedBy");
        return rows.Select(r => new HelperScanRow(r.EventId, r.Person, r.CheckIns, r.CheckOuts)).ToList();
    }

    public sealed class Row
    {
        public int EventId { get; set; }
        public string Person { get; set; } = "";
        public int CheckIns { get; set; }
        public int CheckOuts { get; set; }
    }
}

public sealed class HelperScanReportComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IHelperScanReport, HelperScanReportReader>();
}
