namespace RedAnts.Ticketing.Features.Helpers.Admin;

public sealed record HelperScanRow(int EventId, string Person, int CheckIns, int CheckOuts)
{
    public int Total => CheckIns + CheckOuts;
}
