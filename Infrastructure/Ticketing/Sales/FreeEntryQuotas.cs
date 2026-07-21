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

    public static int? GetFixed(EventFreeEntryQuotaRecord record, FreeEntryType type) => type switch
    {
        FreeEntryType.Player => record.PlayerFixed,
        FreeEntryType.Staff => record.StaffFixed,
        FreeEntryType.Official => record.OfficialFixed,
        FreeEntryType.SwissUnihockeyFreeCard => record.SuFixed,
        FreeEntryType.Child => record.ChildFixed,
        _ => null
    };

    public static void SetFixed(EventFreeEntryQuotaRecord record, FreeEntryType type, int? fixedCount)
    {
        switch (type)
        {
            case FreeEntryType.Player: record.PlayerFixed = fixedCount; break;
            case FreeEntryType.Staff: record.StaffFixed = fixedCount; break;
            case FreeEntryType.Official: record.OfficialFixed = fixedCount; break;
            case FreeEntryType.SwissUnihockeyFreeCard: record.SuFixed = fixedCount; break;
            case FreeEntryType.Child: record.ChildFixed = fixedCount; break;
        }
    }

    public static int FixedTotal(EventFreeEntryQuotaRecord record) =>
        (record.PlayerFixed ?? 0) + (record.StaffFixed ?? 0) + (record.OfficialFixed ?? 0)
        + (record.SuFixed ?? 0) + (record.ChildFixed ?? 0);
}
