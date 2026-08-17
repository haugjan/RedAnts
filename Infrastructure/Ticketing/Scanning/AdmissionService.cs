using Microsoft.Extensions.DependencyInjection;
using NPoco;
using RedAnts.Domain;
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
    IEvents events,
    IEventConversionRules conversionRules) : IAdmissionService
{
    public async Task<Occupancy> GetOccupancyAsync(int eventId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        return await OccAsync(scope.Database, eventId);
    }

    public async Task<ScanOutcome> ScanTicketAsync(int eventId, TicketType type, Guid uuid, int scopeId, ScanMode mode, string? scannedBy, bool test = false)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        var key = uuid.ToString();
        var isEmpty = uuid == Guid.Empty;

        var issued = isEmpty ? null : await tickets.FindAsync(uuid);
        var evaluable = issued is { Status: TicketStatus.Valid } && !test;

        int? eventSeasonId = null;
        if (evaluable && type != TicketType.EventTicket)
            eventSeasonId = (await events.FindByIdAsync(eventId))?.SeasonId;

        int? redeemedEventId = null;
        if (evaluable && type == TicketType.SeasonSingle)
            redeemedEventId = await db.ExecuteScalarAsync<int?>(
                "SELECT RedeemedEventId FROM SeasonSingleTickets WHERE Uuid = @0", key);

        var isMember = type == TicketType.MemberCard;
        var visit = evaluable && !isMember
            ? await db.FirstOrDefaultAsync<EventVisitRecord>("WHERE EventId = @0 AND TicketUuid = @1", eventId, key)
            : null;

        var admissionCap = 1;
        var admissionsInside = visit is { IsInside: true } ? 1 : 0;
        if (evaluable && isMember)
        {
            admissionCap = Math.Max(1, await db.ExecuteScalarAsync<int?>(
                "SELECT Admissions FROM MembershipCards WHERE Uuid = @0", key) ?? 1);
            admissionsInside = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM TicketEventVisits WHERE EventId = @0 AND TicketUuid = @1 AND IsInside = 1",
                eventId, key);
        }

        var requiresConversion = false;
        if (evaluable && type != TicketType.EventTicket)
            requiresConversion = (await conversionRules.GetByEventAsync(eventId)).Any(r => r.CardType == type);

        int? originType = null;
        string? originCardUuid = null;
        if (evaluable && type == TicketType.EventTicket)
        {
            var origin = await db.FirstOrDefaultAsync<EventTicketRecord>("WHERE Uuid = @0", key);
            originType = origin?.OriginType;
            originCardUuid = origin?.OriginCardUuid;
        }

        var facts = issued is null ? null : new ScannedTicketFacts(issued.Type, issued.ScopeId, issued.Status);
        var evaluation = AdmissionRules.Evaluate(
            eventId, type, scopeId, mode, test, isEmpty, facts,
            eventSeasonId, redeemedEventId, admissionsInside, admissionCap, requiresConversion);

        var categoryLabel = issued is null ? null : issued.CategoryName ?? issued.Category?.DisplayName();
        var holder = issued is null ? null : HolderLabel(issued);
        int? usedOut = isMember ? admissionsInside : null;
        int? capOut = isMember ? admissionCap : null;

        switch (evaluation.Verdict)
        {
            case AdmissionVerdict.TestEmpty:
                return new ScanOutcome(AdmissionOutcome.Test, type, "TEST", null,
                    await OccAsync(db, eventId), CategoryLabel: "Scanner-Test");

            case AdmissionVerdict.TestTicket:
                return new ScanOutcome(AdmissionOutcome.Test, type, Ref(uuid), null,
                    await OccAsync(db, eventId), categoryLabel, holder);

            case AdmissionVerdict.Reject when evaluation.Reason is AdmissionRules.AlreadyCheckedIn or AdmissionRules.AllAdmissionsUsed:
                if (isMember)
                {
                    var priors = await MemberPriorsAsync(db, eventId, key);
                    return new ScanOutcome(AdmissionOutcome.Rejected, type, Ref(uuid), evaluation.Reason,
                        await OccAsync(db, eventId), categoryLabel, holder,
                        priors.LastOrDefault()?.At, priors.LastOrDefault()?.By, usedOut, capOut, priors);
                }
                var prior = visit is null ? null : await db.FirstOrDefaultAsync<EventVisitLogRecord>(
                    "WHERE VisitId = @0 AND Type = @1 ORDER BY Id DESC", visit.Id, (int)VisitLogType.CheckIn);
                return new ScanOutcome(AdmissionOutcome.Rejected, type, Ref(uuid), evaluation.Reason,
                    await OccAsync(db, eventId), categoryLabel, holder, prior?.OccurredAt, prior?.ScannedBy);

            case AdmissionVerdict.Reject:
                var carries = AdmissionRules.CarriesHolder(evaluation.Reason);
                return new ScanOutcome(AdmissionOutcome.Rejected, type, Ref(uuid), evaluation.Reason,
                    await OccAsync(db, eventId), carries ? categoryLabel : null, carries ? holder : null);
        }

        if (mode == ScanMode.CheckIn)
        {
            long visitId;
            if (isMember || visit is null)
            {
                var row = new EventVisitRecord
                {
                    EventId = eventId, TicketType = (int)type, TicketUuid = key,
                    IsInside = true, CreatedAt = DateTime.UtcNow,
                    OriginType = originType, OriginCardUuid = originCardUuid
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
                await OccAsync(db, eventId), categoryLabel, holder,
                AdmissionsUsed: isMember ? admissionsInside + 1 : null, AdmissionCap: capOut);
        }

        var checkOut = isMember
            ? await db.FirstOrDefaultAsync<EventVisitRecord>(
                "WHERE EventId = @0 AND TicketUuid = @1 AND IsInside = 1 ORDER BY Id DESC", eventId, key)
            : visit;
        checkOut!.IsInside = false;
        await db.UpdateAsync(checkOut);
        await LogAsync(db, checkOut.Id, AdmissionOutcome.CheckedOut, scannedBy);

        return new ScanOutcome(AdmissionOutcome.CheckedOut, type, Ref(uuid), null,
            await OccAsync(db, eventId), categoryLabel, holder,
            AdmissionsUsed: isMember ? Math.Max(0, admissionsInside - 1) : null, AdmissionCap: capOut);
    }

    private static async Task<IReadOnlyList<PriorScan>> MemberPriorsAsync(IUmbracoDatabase db, int eventId, string key)
    {
        var rows = await db.FetchAsync<EventVisitLogRecord>(
            "SELECT l.* FROM TicketEventVisitsLogs l " +
            "JOIN TicketEventVisits v ON v.Id = l.VisitId " +
            "WHERE v.EventId = @0 AND v.TicketUuid = @1 AND l.Type = @2 ORDER BY l.OccurredAt",
            eventId, key, (int)VisitLogType.CheckIn);
        return rows.Select(r => new PriorScan(r.OccurredAt, r.ScannedBy)).ToList();
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
        var today = SwissTime.Today;
        var age = today.Year - b.Year;
        if (b > today.AddYears(-age)) age--;
        return age is < 0 or > 120 ? null : age;
    }

    public async Task<ScanOutcome> ScanCodeAsync(int eventId, string shortCode, ScanMode mode, string? scannedBy, bool test = false)
    {
        var code = (shortCode ?? "").Trim().Replace(" ", "").ToLowerInvariant();
        if (code.Length != 8 || !code.All(Uri.IsHexDigit))
            return await RejectCodeAsync(eventId, shortCode, "Der Code besteht aus den ersten 8 Zeichen der Ticket-Nr.");

        var resolved = await FindTicketByCodeAsync(code);
        if (resolved is null)
            return await RejectCodeAsync(eventId, code.ToUpperInvariant(), "Kein Ticket mit diesem Code gefunden.");

        var (type, uuid, scopeId) = resolved.Value;
        return await ScanTicketAsync(eventId, type, uuid, scopeId, mode, scannedBy, test);
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
        if (occ.Full && type is FreeEntryType.SwissUnihockeyFreeCard or FreeEntryType.Child)
            return new ScanOutcome(AdmissionOutcome.Rejected, TicketType.FreeEntry, null,
                $"Halle voll — kein Gratiseintritt ({type.DisplayName()}) mehr.", occ);

        var quotaRecord = await db.FirstOrDefaultAsync<EventFreeEntryQuotaRecord>("WHERE EventId = @0", eventId);
        if (quotaRecord is not null && FreeEntryQuotas.Get(quotaRecord, type) is { } q)
        {
            var granted = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM TicketEventFreeEntries f " +
                "JOIN TicketEventVisits v ON v.Id = f.VisitId " +
                "WHERE v.EventId = @0 AND f.FreeEntryType = @1",
                eventId, (int)type);
            var used = granted + (FreeEntryQuotas.GetFixed(quotaRecord, type) ?? 0);
            if (used >= q)
                return new ScanOutcome(AdmissionOutcome.Rejected, TicketType.FreeEntry, null,
                    $"Kontingent für {type.DisplayName()} erschöpft ({used}/{q}).", occ);
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
        var fixedRecord = await db.FirstOrDefaultAsync<EventFreeEntryQuotaRecord>("WHERE EventId = @0", eventId);
        var fixedTotal = fixedRecord is null ? 0 : FreeEntryQuotas.FixedTotal(fixedRecord);
        var tallies = await FreeTalliesAsync(db, eventId, fixedRecord);
        return new Occupancy(inside + fixedTotal, quota, freeInside + fixedTotal, tallies);
    }

    private static async Task<IReadOnlyList<FreeEntryTally>> FreeTalliesAsync(
        IUmbracoDatabase db, int eventId, EventFreeEntryQuotaRecord? fixedRecord)
    {
        var rows = await db.FetchAsync<FreeTallyRow>(
            "SELECT f.FreeEntryType AS FreeEntryType, " +
            "SUM(CASE WHEN v.IsInside = 1 THEN 1 ELSE 0 END) AS Inside, " +
            "SUM(CASE WHEN v.IsInside = 0 THEN 1 ELSE 0 END) AS OutCount " +
            "FROM TicketEventVisits v JOIN TicketEventFreeEntries f ON f.VisitId = v.Id " +
            "WHERE v.EventId = @0 GROUP BY f.FreeEntryType", eventId);

        var tallies = new List<FreeEntryTally>();
        foreach (var type in Enum.GetValues<FreeEntryType>())
        {
            var row = rows.FirstOrDefault(r => r.FreeEntryType == (int)type);
            var fixedCount = fixedRecord is null ? 0 : FreeEntryQuotas.GetFixed(fixedRecord, type) ?? 0;
            var inside = (row?.Inside ?? 0) + fixedCount;
            var outCount = row?.OutCount ?? 0;
            if (inside > 0 || outCount > 0)
                tallies.Add(new FreeEntryTally(type, inside, outCount));
        }
        return tallies;
    }

    private sealed class FreeTallyRow
    {
        public int FreeEntryType { get; set; }
        public int Inside { get; set; }
        public int OutCount { get; set; }
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
