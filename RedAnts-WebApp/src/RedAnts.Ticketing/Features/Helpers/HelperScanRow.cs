namespace RedAnts.Ticketing.Features.Helpers;

public sealed record HelperScanRow(int EventId, string Person, int CheckIns, int CheckOuts)
{
    public int Total => CheckIns + CheckOuts;
}
