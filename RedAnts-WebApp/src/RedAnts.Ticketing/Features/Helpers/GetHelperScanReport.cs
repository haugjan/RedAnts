namespace RedAnts.Ticketing.Features.Helpers;

public static class GetHelperScanReport
{
    public sealed record Query(IReadOnlyCollection<int> EventIds);

    public sealed class Handler(IHelperScanReportReader reader)
    {
        public Task<IReadOnlyList<HelperScanRow>> HandleAsync(Query query) => reader.GetByEventsAsync(query.EventIds);
    }
}
