using RedAnts.Ticketing.Domain.Admission;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Admission.Infrastructure;
using Xunit;

namespace RedAnts.Ticketing.Tests.Admission;

public class FreeEntryQuotaMappingTests
{
    [Fact]
    public void Every_type_gets_its_quota_and_fixed_count()
    {
        var record = new EventFreeEntryQuotaRecord
        {
            PlayerQuota = 20, StaffQuota = 5, OfficialQuota = 4, SuQuota = 10, ChildQuota = 30, HelperQuota = 15,
            PlayerFixed = 2, StaffFixed = 1, OfficialFixed = 3, SuFixed = 4, ChildFixed = 5, HelperFixed = 6
        };

        var quota = FreeEntryQuotaMapping.ToQuota(record);

        Assert.Equal(20, quota.QuotaFor(FreeEntryType.Player));
        Assert.Equal(5, quota.QuotaFor(FreeEntryType.Staff));
        Assert.Equal(4, quota.QuotaFor(FreeEntryType.Official));
        Assert.Equal(10, quota.QuotaFor(FreeEntryType.SwissUnihockeyFreeCard));
        Assert.Equal(30, quota.QuotaFor(FreeEntryType.Child));
        Assert.Equal(15, quota.QuotaFor(FreeEntryType.Helper));
        Assert.Equal(2, quota.FixedFor(FreeEntryType.Player));
        Assert.Equal(1, quota.FixedFor(FreeEntryType.Staff));
        Assert.Equal(3, quota.FixedFor(FreeEntryType.Official));
        Assert.Equal(4, quota.FixedFor(FreeEntryType.SwissUnihockeyFreeCard));
        Assert.Equal(5, quota.FixedFor(FreeEntryType.Child));
        Assert.Equal(6, quota.FixedFor(FreeEntryType.Helper));
        Assert.Equal(21, quota.FixedTotal);
    }

    [Fact]
    public void Missing_values_mean_no_quota_and_no_fixed_count()
    {
        var quota = FreeEntryQuotaMapping.ToQuota(new EventFreeEntryQuotaRecord());

        foreach (var type in Enum.GetValues<FreeEntryType>())
        {
            Assert.Null(quota.QuotaFor(type));
            Assert.Equal(0, quota.FixedFor(type));
        }
        Assert.Equal(0, quota.FixedTotal);
        Assert.True(quota.Allows(FreeEntryType.Child, 1000, new Occupancy(0, null)).IsAllowed);
    }
}
