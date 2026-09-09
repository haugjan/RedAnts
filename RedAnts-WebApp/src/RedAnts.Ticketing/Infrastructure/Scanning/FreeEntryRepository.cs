using NPoco;
using RedAnts.Ticketing.Domain.Admission;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Ports;
using RedAnts.Ticketing.Infrastructure.Sales;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Infrastructure.Scanning;

public sealed class FreeEntryRepository(IScopeProvider scopeProvider) : IFreeEntryRepository
{
    public async Task<FreeEntryQuota> GetQuotaAsync(int eventId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var record = await scope.Database.FirstOrDefaultAsync<EventFreeEntryQuotaRecord>("WHERE EventId = @0", eventId);
        return record is null ? FreeEntryQuota.Unlimited : FreeEntryQuotaMapping.ToQuota(record);
    }

    public async Task SaveQuotaAsync(int eventId, FreeEntryQuota quota)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        var existing = await db.FirstOrDefaultAsync<EventFreeEntryQuotaRecord>("WHERE EventId = @0", eventId);
        var record = existing ?? new EventFreeEntryQuotaRecord { EventId = eventId };
        foreach (var type in Enum.GetValues<FreeEntryType>())
        {
            FreeEntryQuotas.Set(record, type, quota.QuotaFor(type));
            FreeEntryQuotas.SetFixed(record, type, quota.FixedFor(type) is var fixedCount && fixedCount > 0 ? fixedCount : null);
        }
        if (existing is null) await db.InsertAsync(record);
        else await db.UpdateAsync(record);
    }

    public async Task<int> CountGrantedAsync(int eventId, FreeEntryType type)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        return await scope.Database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM TicketEventFreeEntries f " +
            "JOIN TicketEventVisits v ON v.Id = f.VisitId " +
            "WHERE v.EventId = @0 AND f.FreeEntryType = @1",
            eventId, (int)type);
    }

    public async Task<FreeEntry?> FindLatestInsideAsync(int eventId, FreeEntryType type)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        var visit = await db.FirstOrDefaultAsync<EventVisitRecord>(
            "SELECT v.* FROM TicketEventVisits v " +
            "JOIN TicketEventFreeEntries f ON f.VisitId = v.Id " +
            "WHERE v.EventId = @0 AND v.TicketType = @1 AND v.TicketUuid IS NULL AND v.IsInside = 1 " +
            "AND f.FreeEntryType = @2 ORDER BY v.Id DESC",
            eventId, (int)TicketType.FreeEntry, (int)type);
        if (visit is null) return null;

        var logs = await VisitLogRows.FetchAsync(db, [visit.Id]);
        var uuid = Guid.TryParse(visit.Uuid, out var parsed) ? parsed : Guid.Empty;
        return FreeEntry.FromPersistence(visit.Id, eventId, type, uuid, visit.IsInside, visit.CreatedAt, logs.Select(VisitLogRows.Map));
    }

    public async Task SaveAsync(FreeEntry entry)
    {
        using var scope = scopeProvider.CreateScope();
        var db = scope.Database;
        if (entry.IsNew)
        {
            var row = new EventVisitRecord
            {
                EventId = entry.EventId,
                TicketType = (int)TicketType.FreeEntry,
                TicketUuid = null,
                IsInside = entry.IsInside,
                CreatedAt = entry.CreatedAt,
                Uuid = entry.Uuid.ToString()
            };
            await db.InsertAsync(row);
            await db.InsertAsync(new EventFreeEntryRecord { VisitId = row.Id, FreeEntryType = (int)entry.Type });
            await VisitLogRows.InsertAsync(db, row.Id, entry.NewLogs);
            entry.MarkPersisted(row.Id);
        }
        else
        {
            await db.ExecuteAsync("UPDATE TicketEventVisits SET IsInside = @0 WHERE Id = @1", entry.IsInside, entry.VisitId);
            await VisitLogRows.InsertAsync(db, entry.VisitId, entry.NewLogs);
            entry.MarkPersisted(entry.VisitId);
        }
        scope.Complete();
    }
}
