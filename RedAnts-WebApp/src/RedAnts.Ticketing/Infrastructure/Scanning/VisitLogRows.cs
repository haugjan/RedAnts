using NPoco;
using RedAnts.Ticketing.Domain.Admission;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Infrastructure.Sales;

namespace RedAnts.Ticketing.Infrastructure.Scanning;

internal static class VisitLogRows
{
    public static async Task<IReadOnlyList<EventVisitLogRecord>> FetchAsync(IDatabase db, IEnumerable<long> visitIds)
    {
        var ids = visitIds.ToList();
        if (ids.Count == 0) return [];
        return await db.FetchAsync<EventVisitLogRecord>("WHERE VisitId IN (@0) ORDER BY OccurredAt, Id", ids);
    }

    public static VisitLog Map(EventVisitLogRecord row) =>
        new(row.Id, (VisitLogType)row.Type, row.OccurredAt, row.ScannedBy);

    public static async Task InsertAsync(IDatabase db, long visitId, IEnumerable<VisitLog> logs)
    {
        foreach (var log in logs)
            await db.InsertAsync(new EventVisitLogRecord
            {
                VisitId = visitId,
                Type = (int)log.Type,
                OccurredAt = log.OccurredAt,
                ScannedBy = log.ScannedBy
            });
    }
}
