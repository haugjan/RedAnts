using NPoco;
using RedAnts.Ticketing.Domain.Admission;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Ports;
using RedAnts.Ticketing.Infrastructure.Sales;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Infrastructure.Scanning;

public sealed class OccupancyReader(IScopeProvider scopeProvider) : IOccupancyReader
{
    public async Task<Occupancy> GetAsync(int eventId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        var inside = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM TicketEventVisits WHERE EventId = @0 AND IsInside = 1", eventId);
        var quota = await db.ExecuteScalarAsync<int?>(
            "SELECT AdmissionQuota FROM EventPrices WHERE EventId = @0", eventId);
        var freeInside = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM TicketEventVisits WHERE EventId = @0 AND IsInside = 1 AND TicketType = @1",
            eventId, (int)TicketType.FreeEntry);
        var fixedRecord = await db.FirstOrDefaultAsync<EventFreeEntryQuotaRecord>("WHERE EventId = @0", eventId);
        var fixedTotal = fixedRecord is null ? 0 : FreeEntryQuotas.FixedTotal(fixedRecord);
        var tallies = await TalliesAsync(db, eventId, fixedRecord);
        return new Occupancy(inside + fixedTotal, quota, freeInside + fixedTotal, tallies);
    }

    private static async Task<IReadOnlyList<FreeEntryTally>> TalliesAsync(IDatabase db, int eventId, EventFreeEntryQuotaRecord? fixedRecord)
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
}
