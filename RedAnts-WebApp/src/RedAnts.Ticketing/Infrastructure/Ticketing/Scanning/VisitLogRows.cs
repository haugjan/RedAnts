using NPoco;
using RedAnts.Domain.Ticketing.Admission;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Infrastructure.Ticketing.Sales;

namespace RedAnts.Infrastructure.Ticketing.Scanning;

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
