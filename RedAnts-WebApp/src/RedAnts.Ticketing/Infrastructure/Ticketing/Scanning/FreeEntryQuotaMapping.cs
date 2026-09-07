using RedAnts.Domain.Ticketing.Admission;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Infrastructure.Ticketing.Sales;

namespace RedAnts.Infrastructure.Ticketing.Scanning;

internal static class FreeEntryQuotaMapping
{
    public static FreeEntryQuota ToQuota(EventFreeEntryQuotaRecord record)
    {
        var quotas = new Dictionary<FreeEntryType, int?>();
        var fixedCounts = new Dictionary<FreeEntryType, int>();
        foreach (var type in Enum.GetValues<FreeEntryType>())
        {
            quotas[type] = FreeEntryQuotas.Get(record, type);
            fixedCounts[type] = FreeEntryQuotas.GetFixed(record, type) ?? 0;
        }
        return new FreeEntryQuota(quotas, fixedCounts);
    }
}
