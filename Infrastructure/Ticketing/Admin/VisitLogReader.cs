using Microsoft.Extensions.DependencyInjection;
using NPoco;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Admin;
using RedAnts.Features.Ticketing.Ports;
using RedAnts.Infrastructure.Ticketing.Sales;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Admin;

public sealed class VisitLogReader(IScopeProvider scopeProvider, IEvents events) : IVisitLogReader
{
    public async Task<IReadOnlyList<TicketVisitEntry>> GetByTicketUuidAsync(Guid uuid)
    {
        List<EventVisitRecord> visits;
        List<EventVisitLogRecord> logs;
        using (var scope = scopeProvider.CreateScope(autoComplete: true))
        {
            visits = await scope.Database.FetchAsync<EventVisitRecord>(
                "WHERE TicketUuid = @0 ORDER BY CreatedAt", uuid.ToString());
            if (visits.Count == 0) return [];

            var visitIds = visits.Select(v => v.Id).ToArray();
            logs = await scope.Database.FetchAsync<EventVisitLogRecord>(
                $"WHERE VisitId IN ({string.Join(',', visitIds)}) ORDER BY OccurredAt");
        }

        var allEvents = await events.GetAllAsync();
        var eventsById = allEvents.ToDictionary(e => e.Id);
        var logsByVisit = logs.GroupBy(l => l.VisitId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<TicketVisitScan>)g
                .Select(l => new TicketVisitScan((VisitLogType)l.Type, l.OccurredAt, l.ScannedBy))
                .ToList());

        return visits.Select(v =>
        {
            var evt = eventsById.GetValueOrDefault(v.EventId);
            return new TicketVisitEntry(
                v.Id,
                v.EventId,
                evt?.Name ?? $"Anlass {v.EventId}",
                evt?.Date,
                v.IsInside,
                logsByVisit.GetValueOrDefault(v.Id) ?? []);
        }).ToList();
    }
}

public sealed class VisitLogReaderComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IVisitLogReader, VisitLogReader>();
}
