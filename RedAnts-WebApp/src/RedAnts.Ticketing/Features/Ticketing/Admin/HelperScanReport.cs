namespace RedAnts.Features.Ticketing.Admin;

public sealed record HelperScanRow(int EventId, string Person, int CheckIns, int CheckOuts)
{
    public int Total => CheckIns + CheckOuts;
}

public interface IHelperScanReport
{
    Task<IReadOnlyList<HelperScanRow>> GetByEventsAsync(IReadOnlyCollection<int> eventIds);
}
