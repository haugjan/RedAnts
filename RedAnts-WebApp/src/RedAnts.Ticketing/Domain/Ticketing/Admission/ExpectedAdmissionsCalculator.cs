namespace RedAnts.Domain.Ticketing.Admission;

public static class ExpectedAdmissionsCalculator
{
    public static int Calculate(int soldSingleTickets, int seasonPassHolders, int memberHolders, int redeemedFreeEntries) =>
        soldSingleTickets + seasonPassHolders + memberHolders + redeemedFreeEntries;

    public static int Potential(int expectedAdmissions, int? salesQuota, int soldSingleTickets) =>
        salesQuota is { } quota ? expectedAdmissions + Math.Max(0, quota - soldSingleTickets) : int.MaxValue;
}
