namespace RedAnts.Ticketing.Features.Helpers;

public interface IHelperScanReportReader
{
    Task<IReadOnlyList<HelperScanRow>> GetByEventsAsync(IReadOnlyCollection<int> eventIds);
}
