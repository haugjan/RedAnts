namespace RedAnts.Ticketing.Features.Admission;

public static class GetFreeEntries
{
    public sealed record Query(int EventId);

    public sealed class Handler(IFreeEntryListReader freeEntries)
    {
        public async Task<IReadOnlyList<FreeEntryRow>> HandleAsync(Query query) =>
            query.EventId <= 0 ? [] : await freeEntries.GetByEventAsync(query.EventId);
    }
}
