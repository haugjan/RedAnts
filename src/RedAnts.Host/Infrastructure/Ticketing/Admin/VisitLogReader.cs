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
    public async Task<IReadOnlyDictionary<Guid, bool>> GetInsideByEventAsync(int eventId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var visits = await scope.Database.FetchAsync<EventVisitRecord>(
            "WHERE EventId = @0 AND TicketUuid IS NOT NULL", eventId);
        var map = new Dictionary<Guid, bool>();
        foreach (var v in visits)
            if (Guid.TryParse(v.TicketUuid, out var uuid))
                map[uuid] = v.IsInside;
        return map;
    }

    public async Task<IReadOnlyList<TicketVisitEntry>> GetByTicketUuidAsync(Guid uuid)
    {
        List<EventVisitRecord> visits;
        List<EventVisitLogRecord> logs;
        List<EventTicketRecord> conversions;
        List<EventVisitRecord> convVisits = [];
        List<EventVisitLogRecord> convLogs = [];
        using (var scope = scopeProvider.CreateScope(autoComplete: true))
        {
            visits = await scope.Database.FetchAsync<EventVisitRecord>(
                "WHERE TicketUuid = @0 ORDER BY CreatedAt", uuid.ToString());

            conversions = await scope.Database.FetchAsync<EventTicketRecord>(
                "WHERE OriginCardUuid = @0 AND Status = @1 ORDER BY CreatedAt",
                uuid.ToString(), (int)TicketStatus.Valid);

            if (visits.Count == 0 && conversions.Count == 0) return [];

            if (visits.Count > 0)
                logs = await scope.Database.FetchAsync<EventVisitLogRecord>(
                    $"WHERE VisitId IN ({string.Join(',', visits.Select(v => v.Id))}) ORDER BY OccurredAt");
            else
                logs = [];

            if (conversions.Count > 0)
            {
                var convUuids = string.Join(",", conversions.Select(c => $"'{c.Uuid}'"));
                convVisits = await scope.Database.FetchAsync<EventVisitRecord>(
                    $"WHERE TicketUuid IN ({convUuids}) ORDER BY CreatedAt");
                if (convVisits.Count > 0)
                    convLogs = await scope.Database.FetchAsync<EventVisitLogRecord>(
                        $"WHERE VisitId IN ({string.Join(',', convVisits.Select(v => v.Id))}) ORDER BY OccurredAt");
            }
        }

        var allEvents = await events.GetAllAsync();
        var eventsById = allEvents.ToDictionary(e => e.Id);

        IReadOnlyList<TicketVisitScan> ScansFor(long visitId, IEnumerable<EventVisitLogRecord> source) =>
            source.Where(l => l.VisitId == visitId)
                .Select(l => new TicketVisitScan((VisitLogType)l.Type, l.OccurredAt, l.ScannedBy))
                .ToList();

        var result = new List<TicketVisitEntry>();

        foreach (var v in visits)
        {
            var evt = eventsById.GetValueOrDefault(v.EventId);
            result.Add(new TicketVisitEntry(
                v.Id, v.EventId, evt?.Name ?? $"Anlass {v.EventId}", evt?.Date,
                v.IsInside, ScansFor(v.Id, logs)));
        }

        foreach (var c in conversions)
        {
            var evt = eventsById.GetValueOrDefault(c.EventId);
            var name = evt?.Name ?? $"Anlass {c.EventId}";
            Guid? ticketUuid = Guid.TryParse(c.Uuid, out var tu) ? tu : null;
            result.Add(new TicketVisitEntry(
                0, c.EventId, name, evt?.Date, false, [],
                TicketVisitKind.Conversion, c.CreatedAt, TicketUuid: ticketUuid));

            foreach (var cv in convVisits.Where(x =>
                string.Equals(x.TicketUuid, c.Uuid, StringComparison.OrdinalIgnoreCase)))
            {
                result.Add(new TicketVisitEntry(
                    cv.Id, cv.EventId, name, evt?.Date, cv.IsInside,
                    ScansFor(cv.Id, convLogs), TicketVisitKind.Visit, null, ViaConversion: true, TicketUuid: ticketUuid));
            }
        }

        return result;
    }
}

public sealed class VisitLogReaderComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IVisitLogReader, VisitLogReader>();
}
