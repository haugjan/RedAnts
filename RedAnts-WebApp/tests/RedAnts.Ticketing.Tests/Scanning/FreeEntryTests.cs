using RedAnts.Domain.Ticketing.Admission;
using RedAnts.Domain.Ticketing.Sales;
using Xunit;

namespace RedAnts.Ticketing.Tests.Scanning;

public class FreeEntryTests
{
    private const int Event = 100;
    private static readonly DateTime Now = new(2026, 9, 7, 18, 0, 0, DateTimeKind.Utc);

    private static readonly Occupancy Open = new(10, 100);
    private static readonly Occupancy Full = new(100, 100);

    [Fact]
    public void Grant_creates_an_inside_entry_with_a_fresh_uuid_and_one_check_in_log()
    {
        var first = FreeEntry.Grant(Event, FreeEntryType.Child, "Kasse", Now);
        var second = FreeEntry.Grant(Event, FreeEntryType.Child, "Kasse", Now);

        Assert.True(first.IsNew);
        Assert.True(first.IsInside);
        Assert.Equal(Event, first.EventId);
        Assert.Equal(FreeEntryType.Child, first.Type);
        Assert.Equal(Now, first.CreatedAt);
        Assert.NotEqual(Guid.Empty, first.Uuid);
        Assert.NotEqual(first.Uuid, second.Uuid);
        var log = Assert.Single(first.NewLogs);
        Assert.Equal(VisitLogType.CheckIn, log.Type);
        Assert.Equal(Now, log.OccurredAt);
        Assert.Equal("Kasse", log.ScannedBy);
        Assert.Single(first.Logs);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Grant_rejects_a_missing_event(int eventId) =>
        Assert.Throws<DomainException>(() => FreeEntry.Grant(eventId, FreeEntryType.Player, null, Now));

    [Fact]
    public void Revoke_marks_the_entry_outside_and_logs_the_check_out()
    {
        var entry = FreeEntry.Grant(Event, FreeEntryType.Helper, "Kasse", Now);

        entry.Revoke("Tor", Now.AddHours(1));

        Assert.False(entry.IsInside);
        Assert.Equal(2, entry.NewLogs.Count);
        Assert.Equal(VisitLogType.CheckOut, entry.NewLogs[1].Type);
        Assert.Equal(Now.AddHours(1), entry.NewLogs[1].OccurredAt);
        Assert.Equal("Tor", entry.NewLogs[1].ScannedBy);
    }

    [Fact]
    public void Revoke_when_already_outside_throws()
    {
        var entry = FreeEntry.FromPersistence(5, Event, FreeEntryType.Staff, Guid.NewGuid(), false, Now,
            [new VisitLog(1, VisitLogType.CheckIn, Now, null), new VisitLog(2, VisitLogType.CheckOut, Now.AddMinutes(1), null)]);

        var ex = Assert.Throws<DomainException>(() => entry.Revoke("Tor", Now.AddHours(1)));

        Assert.Contains("Staff", ex.Message);
        Assert.Empty(entry.NewLogs);
    }

    [Fact]
    public void Mark_persisted_sets_the_visit_id_and_clears_new_logs()
    {
        var entry = FreeEntry.Grant(Event, FreeEntryType.Official, null, Now);

        entry.MarkPersisted(77);

        Assert.Equal(77, entry.VisitId);
        Assert.False(entry.IsNew);
        Assert.Empty(entry.NewLogs);
        Assert.Single(entry.Logs);
    }

    [Fact]
    public void From_persistence_orders_logs_by_time()
    {
        var uuid = Guid.NewGuid();
        var entry = FreeEntry.FromPersistence(3, Event, FreeEntryType.Player, uuid, true, Now,
            [new VisitLog(2, VisitLogType.CheckOut, Now.AddMinutes(5), "B"), new VisitLog(1, VisitLogType.CheckIn, Now, "A")]);

        Assert.Equal(uuid, entry.Uuid);
        Assert.Equal("A", entry.Logs[0].ScannedBy);
        Assert.Equal("B", entry.Logs[1].ScannedBy);
        Assert.Empty(entry.NewLogs);
    }

    [Theory]
    [InlineData(FreeEntryType.SwissUnihockeyFreeCard)]
    [InlineData(FreeEntryType.Child)]
    public void A_full_hall_denies_free_cards_and_children(FreeEntryType type)
    {
        var result = FreeEntryQuota.Unlimited.Allows(type, 0, Full);

        var denied = Assert.IsType<CheckResult.Denied>(result);
        var reason = Assert.IsType<FreeEntryDenied.HallFull>(denied.Cause);
        Assert.Equal(type, reason.Type);
        Assert.Contains("Halle voll", reason.Message);
        Assert.Contains(type.DisplayName(), reason.Message);
    }

    [Theory]
    [InlineData(FreeEntryType.Player)]
    [InlineData(FreeEntryType.Staff)]
    [InlineData(FreeEntryType.Official)]
    [InlineData(FreeEntryType.Helper)]
    public void A_full_hall_still_admits_teams_officials_and_helpers(FreeEntryType type) =>
        Assert.True(FreeEntryQuota.Unlimited.Allows(type, 0, Full).IsAllowed);

    [Fact]
    public void Quota_counts_granted_plus_fixed_and_denies_when_reached()
    {
        var quota = new FreeEntryQuota(
            new Dictionary<FreeEntryType, int?> { [FreeEntryType.Player] = 20 },
            new Dictionary<FreeEntryType, int> { [FreeEntryType.Player] = 15 });

        var result = quota.Allows(FreeEntryType.Player, 5, Open);

        var denied = Assert.IsType<CheckResult.Denied>(result);
        var reason = Assert.IsType<FreeEntryDenied.QuotaExhausted>(denied.Cause);
        Assert.Equal(FreeEntryType.Player, reason.Type);
        Assert.Equal(20, reason.Used);
        Assert.Equal(20, reason.Quota);
        Assert.Contains("(20/20)", reason.Message);
    }

    [Fact]
    public void Quota_allows_while_granted_plus_fixed_stay_below_it()
    {
        var quota = new FreeEntryQuota(
            new Dictionary<FreeEntryType, int?> { [FreeEntryType.Player] = 20 },
            new Dictionary<FreeEntryType, int> { [FreeEntryType.Player] = 15 });

        Assert.True(quota.Allows(FreeEntryType.Player, 4, Open).IsAllowed);
    }

    [Fact]
    public void Quota_only_applies_to_its_own_type()
    {
        var quota = new FreeEntryQuota(
            new Dictionary<FreeEntryType, int?> { [FreeEntryType.Player] = 1 },
            new Dictionary<FreeEntryType, int> { [FreeEntryType.Player] = 1 });

        Assert.True(quota.Allows(FreeEntryType.Staff, 50, Open).IsAllowed);
        Assert.False(quota.Allows(FreeEntryType.Player, 0, Open).IsAllowed);
    }

    [Fact]
    public void A_null_quota_means_no_limit()
    {
        var quota = new FreeEntryQuota(
            new Dictionary<FreeEntryType, int?> { [FreeEntryType.Helper] = null },
            new Dictionary<FreeEntryType, int>());

        Assert.True(quota.Allows(FreeEntryType.Helper, 500, Open).IsAllowed);
        Assert.Null(quota.QuotaFor(FreeEntryType.Helper));
    }

    [Fact]
    public void Unlimited_allows_every_type_in_an_open_hall()
    {
        foreach (var type in Enum.GetValues<FreeEntryType>())
            Assert.True(FreeEntryQuota.Unlimited.Allows(type, 1000, Open).IsAllowed);
    }

    [Fact]
    public void Fixed_total_sums_all_fixed_counts_and_lookups_default_to_zero()
    {
        var quota = new FreeEntryQuota(
            new Dictionary<FreeEntryType, int?>(),
            new Dictionary<FreeEntryType, int> { [FreeEntryType.Player] = 12, [FreeEntryType.Staff] = 3, [FreeEntryType.Official] = 2 });

        Assert.Equal(17, quota.FixedTotal);
        Assert.Equal(12, quota.FixedFor(FreeEntryType.Player));
        Assert.Equal(0, quota.FixedFor(FreeEntryType.Child));
        Assert.Equal(0, FreeEntryQuota.Unlimited.FixedTotal);
    }
}
