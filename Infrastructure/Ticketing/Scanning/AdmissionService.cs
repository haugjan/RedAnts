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

    public async Task<ScanOutcome> ScanTicketAsync(int eventId, TicketType type, Guid uuid, int scopeId, ScanMode mode, string? scannedBy)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        var key = uuid.ToString();

        async Task<ScanOutcome> Reject(string reason) =>
            new(AdmissionOutcome.Rejected, type, Ref(uuid), reason, await OccAsync(db, eventId));

        var issued = await tickets.FindAsync(uuid);
        if (issued is null) return await Reject("Unbekanntes Ticket.");
        if (issued.Type != type || issued.ScopeId != scopeId)
            return await Reject("Ticket stimmt nicht mit dem Datensatz überein.");
        if (issued.Status != TicketStatus.Valid)
            return await Reject(issued.Status == TicketStatus.Blocked ? "Ticket ist gesperrt." : "Ticket ist storniert.");

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

        var categoryLabel = issued.Category?.DisplayName();
        var holder = HolderLabel(issued);

        var visit = await db.FirstOrDefaultAsync<EventVisitRecord>(
            "WHERE EventId = @0 AND TicketUuid = @1", eventId, key);

        if (mode == ScanMode.CheckIn)
        {
            if (visit is { IsInside: true })
            {
                var prior = await db.FirstOrDefaultAsync<EventVisitLogRecord>(
                    "WHERE VisitId = @0 AND Type = @1 ORDER BY Id DESC", visit.Id, (int)VisitLogType.CheckIn);
                return new ScanOutcome(AdmissionOutcome.Rejected, type, Ref(uuid), "Bereits eingecheckt.",
                    await OccAsync(db, eventId), categoryLabel, holder, prior?.OccurredAt, prior?.ScannedBy);
            }

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
            }
            else
            {
                visit.IsInside = true;
                await db.UpdateAsync(visit);
                visitId = visit.Id;
            }
            await LogAsync(db, visitId, AdmissionOutcome.CheckedIn, scannedBy);

            if (type == TicketType.SeasonSingle)
                await db.ExecuteAsync(
                    "UPDATE SeasonSingleTickets SET RedeemedEventId = @0, Redeemed = 1 WHERE Uuid = @1 AND RedeemedEventId IS NULL",
                    eventId, key);

            if (type == TicketType.EventTicket)
                await db.ExecuteAsync("UPDATE EventTickets SET Redeemed = 1 WHERE Uuid = @0", key);

            return new ScanOutcome(AdmissionOutcome.CheckedIn, type, Ref(uuid), null,
                await OccAsync(db, eventId), categoryLabel, holder);
        }

        if (visit is null || !visit.IsInside)
            return new ScanOutcome(AdmissionOutcome.Rejected, type, Ref(uuid), "Noch nicht eingecheckt.",
                await OccAsync(db, eventId), categoryLabel, holder);

        visit.IsInside = false;
        await db.UpdateAsync(visit);
        await LogAsync(db, visit.Id, AdmissionOutcome.CheckedOut, scannedBy);

        return new ScanOutcome(AdmissionOutcome.CheckedOut, type, Ref(uuid), null,
            await OccAsync(db, eventId), categoryLabel, holder);
    }

    private static string? HolderLabel(IssuedTicket ticket)
    {
        if (ticket.Type == TicketType.MemberCard)
        {
            if (string.IsNullOrWhiteSpace(ticket.HolderName)) return null;
            return Age(ticket.Birthday) is { } age ? $"{ticket.HolderName} ({age})" : ticket.HolderName;
        }
        return string.IsNullOrWhiteSpace(ticket.BuyerName) ? null : ticket.BuyerName;
    }

    private static int? Age(DateOnly? birthday)
    {
        if (birthday is not { } b) return null;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - b.Year;
        if (b > today.AddYears(-age)) age--;
        return age is < 0 or > 120 ? null : age;
    }

    public async Task<ScanOutcome> ScanCodeAsync(int eventId, string shortCode, ScanMode mode, string? scannedBy)
    {
        var code = (shortCode ?? "").Trim().Replace(" ", "").ToLowerInvariant();
        if (code.Length != 8 || !code.All(Uri.IsHexDigit))
            return await RejectCodeAsync(eventId, shortCode, "Der Code besteht aus den ersten 8 Zeichen der Ticket-Nr.");

        var resolved = await FindTicketByCodeAsync(code);
        if (resolved is null)
            return await RejectCodeAsync(eventId, code.ToUpperInvariant(), "Kein Ticket mit diesem Code gefunden.");

        var (type, uuid, scopeId) = resolved.Value;
        return await ScanTicketAsync(eventId, type, uuid, scopeId, mode, scannedBy);
    }

    private async Task<ScanOutcome> RejectCodeAsync(int eventId, string? reference, string reason)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        return new ScanOutcome(AdmissionOutcome.Rejected, null, reference?.Trim().ToUpperInvariant(),
            reason, await OccAsync(scope.Database, eventId));
    }

    private async Task<(TicketType Type, Guid Uuid, int ScopeId)?> FindTicketByCodeAsync(string code)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        var pattern = code + "%";

        var eventTicket = await db.FirstOrDefaultAsync<EventTicketRecord>("WHERE Uuid LIKE @0", pattern);
        if (eventTicket is not null && Guid.TryParse(eventTicket.Uuid, out var eventUuid))
            return (TicketType.EventTicket, eventUuid, eventTicket.EventId);

        var single = await db.FirstOrDefaultAsync<SeasonSingleTicketRecord>("WHERE Uuid LIKE @0", pattern);
        if (single is not null && Guid.TryParse(single.Uuid, out var singleUuid))
            return (TicketType.SeasonSingle, singleUuid, single.SeasonId);

        var pass = await db.FirstOrDefaultAsync<SeasonPassRecord>("WHERE Uuid LIKE @0", pattern);
        if (pass is not null && Guid.TryParse(pass.Uuid, out var passUuid))
            return (TicketType.SeasonPass, passUuid, pass.SeasonId);

        var card = await db.FirstOrDefaultAsync<MemberCardRecord>("WHERE Uuid LIKE @0", pattern);
        if (card is not null && Guid.TryParse(card.Uuid, out var cardUuid))
            return (TicketType.MemberCard, cardUuid, card.SeasonId);

        return null;
    }

    public async Task<ScanOutcome> GrantFreeEntryAsync(int eventId, FreeEntryType type, string? scannedBy)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;

        var occ = await OccAsync(db, eventId);
        if (occ.Full)
            return new ScanOutcome(AdmissionOutcome.Rejected, TicketType.FreeEntry, null, "Halle ist voll.", occ);

        if (type == FreeEntryType.SwissUnihockeyFreeCard)
        {
            var quota = await db.ExecuteScalarAsync<int?>(
                "SELECT SuQuota FROM TicketEventFreeEntryQuotas WHERE EventId = @0", eventId);
            if (quota is { } q)
            {
                var granted = await db.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM TicketEventFreeEntries f " +
                    "JOIN TicketEventVisits v ON v.Id = f.VisitId " +
                    "WHERE v.EventId = @0 AND f.FreeEntryType = @1",
                    eventId, (int)FreeEntryType.SwissUnihockeyFreeCard);
                if (granted >= q)
                    return new ScanOutcome(AdmissionOutcome.Rejected, TicketType.FreeEntry, null,
                        $"SU-Freieintritt-Kontingent erschöpft ({granted}/{q}).", occ);
            }
        }

        var row = new EventVisitRecord
        {
            EventId = eventId, TicketType = (int)TicketType.FreeEntry, TicketUuid = null,
            IsInside = true, CreatedAt = DateTime.UtcNow, Uuid = Guid.NewGuid().ToString()
        };
        await db.InsertAsync(row);
        await db.InsertAsync(new EventFreeEntryRecord { VisitId = row.Id, FreeEntryType = (int)type });
        await LogAsync(db, row.Id, AdmissionOutcome.CheckedIn, scannedBy);

        return new ScanOutcome(AdmissionOutcome.CheckedIn, TicketType.FreeEntry, type.DisplayName(), null,
            await OccAsync(db, eventId));
    }

    public async Task<ScanOutcome> RevokeFreeEntryAsync(int eventId, FreeEntryType type, string? scannedBy)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;

        var visit = await db.FirstOrDefaultAsync<EventVisitRecord>(
            "SELECT v.* FROM TicketEventVisits v " +
            "JOIN TicketEventFreeEntries f ON f.VisitId = v.Id " +
            "WHERE v.EventId = @0 AND v.TicketType = @1 AND v.TicketUuid IS NULL AND v.IsInside = 1 " +
            "AND f.FreeEntryType = @2 ORDER BY v.Id DESC",
            eventId, (int)TicketType.FreeEntry, (int)type);
        if (visit is null)
            return new ScanOutcome(AdmissionOutcome.Rejected, TicketType.FreeEntry, type.DisplayName(),
                $"Kein freier Einlass ({type.DisplayName()}) zum Auschecken.", await OccAsync(db, eventId));

        visit.IsInside = false;
        await db.UpdateAsync(visit);
        await LogAsync(db, visit.Id, AdmissionOutcome.CheckedOut, scannedBy);

        return new ScanOutcome(AdmissionOutcome.CheckedOut, TicketType.FreeEntry, type.DisplayName(), null,
            await OccAsync(db, eventId));
    }

    private static async Task<Occupancy> OccAsync(IUmbracoDatabase db, int eventId)
    {
        var inside = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM TicketEventVisits WHERE EventId = @0 AND IsInside = 1", eventId);
        var quota = await db.ExecuteScalarAsync<int?>(
            "SELECT AdmissionQuota FROM EventPrices WHERE EventId = @0", eventId);
        var freeInside = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM TicketEventVisits WHERE EventId = @0 AND IsInside = 1 AND TicketType = @1",
            eventId, (int)TicketType.FreeEntry);
        return new Occupancy(inside, quota, freeInside);
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
