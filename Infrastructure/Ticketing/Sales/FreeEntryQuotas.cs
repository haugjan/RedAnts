using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Infrastructure.Ticketing.Sales;

public static class FreeEntryQuotas
{
    public static int? Get(EventFreeEntryQuotaRecord record, FreeEntryType type) => type switch
    {
        FreeEntryType.Player => record.PlayerQuota,
        FreeEntryType.Staff => record.StaffQuota,
        FreeEntryType.Official => record.OfficialQuota,
        FreeEntryType.SwissUnihockeyFreeCard => record.SuQuota,
        FreeEntryType.Child => record.ChildQuota,
        _ => null
    };

    public static void Set(EventFreeEntryQuotaRecord record, FreeEntryType type, int? quota)
    {
        switch (type)
        {
            case FreeEntryType.Player: record.PlayerQuota = quota; break;
            case FreeEntryType.Staff: record.StaffQuota = quota; break;
            case FreeEntryType.Official: record.OfficialQuota = quota; break;
            case FreeEntryType.SwissUnihockeyFreeCard: record.SuQuota = quota; break;
            case FreeEntryType.Child: record.ChildQuota = quota; break;
        }
    }
}
