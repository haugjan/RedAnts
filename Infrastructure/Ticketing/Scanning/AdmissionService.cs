using Microsoft.Extensions.DependencyInjection;
using NPoco;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using RedAnts.Features.Ticketing.Scanning;
using RedAnts.Features.Ticketing.Tickets;
using RedAnts.Infrastructure.Ticketing.Sales;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Scanning;

public sealed class AdmissionService(
    IScopeProvider scopeProvider,
    IIssuedTicketReader tickets,
    IEvents events) : IAdmissionService
{
    public async Task<Occupancy> GetOccupancyAsync(int eventId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        return await OccAsync(scope.Database, eventId);
    }

    public async Task<ScanOutcome> ScanTicketAsync(int eventId, TicketType type, Guid uuid, int scopeId, string? scannedBy)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        var key = uuid.ToString();

        async Task<ScanOutcome> Reject(string reason) =>
            new(AdmissionOutcome.Rejected, type, Ref(uuid), reason, await OccAsync(db, eventId));

        var issued = await tickets.FindAsync(uuid);
        if (issued is null) return await Reject("Unbekanntes Ticket.");
        if (issued.Status != TicketStatus.Valid) return await Reject("Ticket ist storniert.");

        if (type == TicketType.EventTicket)
        {
            if (scopeId != eventId) return await Reject("Ticket gilt für einen anderen Anlass.");
        }
        else
        {
            var ev = await events.FindByIdAsync(eventId);
            if (ev is null) return await Reject("Anlass unbekannt.");
            if (scopeId != ev.SeasonId) return await Reject("Ticket gilt für eine andere Saison.");
        }

        if (type == TicketType.SeasonSingle)
        {
            var bound = await db.ExecuteScalarAsync<int?>(
                "SELECT RedeemedEventId FROM SeasonSingleTickets WHERE Uuid = @0", key);
            if (bound is { } b && b != eventId)
                return await Reject("Flexticket wurde bereits an einem anderen Anlass eingelöst.");
        }

        var visit = await db.FirstOrDefaultAsync<EventVisitRecord>(
            "WHERE EventId = @0 AND TicketUuid = @1", eventId, key);
        AdmissionOutcome action;
        long visitId;
        if (visit is null)
        {
            var row = new EventVisitRecord
            {
                EventId = eventId, TicketType = (int)type, TicketUuid = key,
                IsInside = true, CreatedAt = DateTime.UtcNow
            };
            await db.InsertAsync(row);
            visitId = row.Id;
            action = AdmissionOutcome.CheckedIn;
        }
        else
        {
            visit.IsInside = !visit.IsInside;
            await db.UpdateAsync(visit);
            visitId = visit.Id;
            action = visit.IsInside ? AdmissionOutcome.CheckedIn : AdmissionOutcome.CheckedOut;
        }
        await LogAsync(db, visitId, action, scannedBy);

        if (type == TicketType.SeasonSingle && action == AdmissionOutcome.CheckedIn)
            await db.ExecuteAsync(
                "UPDATE SeasonSingleTickets SET RedeemedEventId = @0, Redeemed = 1 WHERE Uuid = @1 AND RedeemedEventId IS NULL",
                eventId, key);

        return new ScanOutcome(action, type, Ref(uuid), null, await OccAsync(db, eventId));
    }

    public async Task<ScanOutcome> GrantFreeEntryAsync(int eventId, string? scannedBy)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;

        var occ = await OccAsync(db, eventId);
        if (occ.Full)
            return new ScanOutcome(AdmissionOutcome.Rejected, TicketType.FreeEntry, null, "Halle ist voll.", occ);

        var row = new EventVisitRecord
        {
            EventId = eventId, TicketType = (int)TicketType.FreeEntry, TicketUuid = null,
            IsInside = true, CreatedAt = DateTime.UtcNow
        };
        await db.InsertAsync(row);
        await LogAsync(db, row.Id, AdmissionOutcome.CheckedIn, scannedBy);

        return new ScanOutcome(AdmissionOutcome.CheckedIn, TicketType.FreeEntry, "Frei", null, await OccAsync(db, eventId));
    }

    public async Task<ScanOutcome> RevokeFreeEntryAsync(int eventId, string? scannedBy)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;

        var visit = await db.FirstOrDefaultAsync<EventVisitRecord>(
            "WHERE EventId = @0 AND TicketType = @1 AND TicketUuid IS NULL AND IsInside = 1 ORDER BY Id DESC",
            eventId, (int)TicketType.FreeEntry);
        if (visit is null)
            return new ScanOutcome(AdmissionOutcome.Rejected, TicketType.FreeEntry, null,
                "Kein freier Einlass zum Auschecken.", await OccAsync(db, eventId));

        visit.IsInside = false;
        await db.UpdateAsync(visit);
        await LogAsync(db, visit.Id, AdmissionOutcome.CheckedOut, scannedBy);

        return new ScanOutcome(AdmissionOutcome.CheckedOut, TicketType.FreeEntry, "Frei", null, await OccAsync(db, eventId));
    }

    private static async Task<Occupancy> OccAsync(IUmbracoDatabase db, int eventId)
    {
        var inside = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM TicketEventVisits WHERE EventId = @0 AND IsInside = 1", eventId);
        var quota = await db.ExecuteScalarAsync<int?>(
            "SELECT AdmissionQuota FROM EventPrices WHERE EventId = @0", eventId);
        return new Occupancy(inside, quota);
    }

    private static async Task LogAsync(IUmbracoDatabase db, long visitId, AdmissionOutcome action, string? by) =>
        await db.InsertAsync(new EventVisitLogRecord
        {
            VisitId = visitId,
            Type = (int)(action == AdmissionOutcome.CheckedIn ? VisitLogType.CheckIn : VisitLogType.CheckOut),
            OccurredAt = DateTime.UtcNow,
            ScannedBy = by
        });

    private static string Ref(Guid uuid) => uuid.ToString("N")[..8].ToUpperInvariant();
}

public sealed class AdmissionServiceComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IAdmissionService, AdmissionService>();
}
