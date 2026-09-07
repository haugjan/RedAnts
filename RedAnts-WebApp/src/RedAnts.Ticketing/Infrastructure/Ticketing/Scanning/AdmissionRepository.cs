using NPoco;
using RedAnts.Domain.Ticketing.Admission;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using RedAnts.Infrastructure.Ticketing.Sales;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Scanning;

public sealed class AdmissionRepository(IScopeProvider scopeProvider) : IAdmissionRepository
{
    public async Task<Admission> LoadAsync(int eventId, TicketType ticketType, Guid ticketUuid)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        var rows = await db.FetchAsync<EventVisitRecord>(
            "WHERE EventId = @0 AND TicketUuid = @1 ORDER BY Id", eventId, ticketUuid.ToString());
        if (rows.Count == 0) return Admission.None(eventId, ticketType, ticketUuid);

        var logsByVisit = (await VisitLogRows.FetchAsync(db, rows.Select(r => r.Id))).ToLookup(l => l.VisitId);
        var visits = rows.Select(r => AdmissionVisit.FromPersistence(
            r.Id, r.IsInside, r.CreatedAt, r.OriginType, r.OriginCardUuid, logsByVisit[r.Id].Select(VisitLogRows.Map)));
        return Admission.FromPersistence(eventId, ticketType, ticketUuid, visits);
    }

    public async Task SaveAsync(Admission admission)
    {
        using var scope = scopeProvider.CreateScope();
        var db = scope.Database;
        var key = admission.TicketUuid.ToString();
        foreach (var visit in admission.Visits)
        {
            if (visit.IsNew)
            {
                var row = new EventVisitRecord
                {
                    EventId = admission.EventId,
                    TicketType = (int)admission.TicketType,
                    TicketUuid = key,
                    IsInside = visit.IsInside,
                    CreatedAt = visit.CreatedAt,
                    OriginType = visit.OriginType,
                    OriginCardUuid = visit.OriginCardUuid
                };
                await db.InsertAsync(row);
                await VisitLogRows.InsertAsync(db, row.Id, visit.NewLogs);
                visit.MarkPersisted(row.Id);
            }
            else if (visit.Changed)
            {
                await db.ExecuteAsync("UPDATE TicketEventVisits SET IsInside = @0 WHERE Id = @1", visit.IsInside, visit.Id);
                await VisitLogRows.InsertAsync(db, visit.Id, visit.NewLogs);
                visit.MarkPersisted(visit.Id);
            }
        }
        scope.Complete();
    }
}
