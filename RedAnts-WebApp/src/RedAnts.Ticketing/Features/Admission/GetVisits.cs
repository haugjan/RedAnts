namespace RedAnts.Ticketing.Features.Admission;

public static class GetVisits
{
    public sealed record Query(Guid Uuid);

    public sealed class Handler(IVisitLogReader visits)
    {
        public Task<IReadOnlyList<TicketVisitEntry>> HandleAsync(Query query) => visits.GetByTicketUuidAsync(query.Uuid);
    }
}
