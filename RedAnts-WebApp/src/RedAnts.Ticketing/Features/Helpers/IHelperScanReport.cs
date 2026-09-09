using RedAnts.Ticketing.Features.Helpers.Admin;

namespace RedAnts.Ticketing.Features.Helpers;

public interface IHelperScanReport
{
    Task<IReadOnlyList<HelperScanRow>> GetByEventsAsync(IReadOnlyCollection<int> eventIds);
}
