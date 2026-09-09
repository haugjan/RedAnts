using RedAnts.Ticketing.Domain.Admission;
using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Admission.Infrastructure;

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
