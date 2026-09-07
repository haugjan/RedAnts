using RedAnts.Domain.Ticketing.Sales;
using Xunit;

namespace RedAnts.Ticketing.Tests.Sales;

public class CapacityReservationRulesTests
{
    private static readonly DateOnly Today = new(2026, 9, 7);

    private static CapacityUsage Usage(int total, params (int TierId, int Sold)[] byTier) =>
        new(total, byTier.ToDictionary(t => t.TierId, t => t.Sold));

    private static EventPrice Event(int? totalQuota, int reserved = 0, int version = 0, params CategoryPrice[] categories) =>
        EventPrice.FromPersistence(1, 10, totalQuota, null, categories, reserved: reserved, version: version);

    private static CategoryPrice Tier(int tierId, int? quota, int reserved = 0, DateOnly? until = null) =>
        CategoryPrice.FromPersistence(TicketCategory.Adult, 25m, quota, until, tierId, reserved);

    private static SeasonPrice Season(int? totalQuota, int reserved = 0, params SeasonCategoryPrice[] categories) =>
        SeasonPrice.FromPersistence(1, 7, totalQuota, categories, reserved: reserved, version: 4);

    private static SeasonCategoryPrice PassTier(int tierId, int? quota, bool offered = true, int reserved = 0,
        DateOnly? from = null, DateOnly? until = null) =>
        SeasonCategoryPrice.FromPersistence(TicketCategory.Adult, 300m, offered, quota, 25m, true, null, from, until, null, tierId, reserved);

    [Fact]
    public void Event_reserve_allows_within_all_quotas_and_increments_counters()
    {
        var price = Event(100, reserved: 5, version: 3, Tier(3, 20, reserved: 2), Tier(4, null));

        var result = price.Reserve([new TierDemand(3, 4), new TierDemand(4, 6)], Usage(50, (3, 10)), Today);

        Assert.True(result.IsAllowed);
        Assert.Equal(15, price.Reserved);
        Assert.Equal(6, price.Categories[0].Reserved);
        Assert.Equal(6, price.Categories[1].Reserved);
        Assert.Equal(3, price.Version);
    }

    [Fact]
    public void Event_reserve_respects_total_quota_including_sold_and_reserved()
    {
        var price = Event(100, reserved: 20, version: 0, Tier(3, null));

        var denied = price.Reserve([new TierDemand(3, 11)], Usage(70), Today);
        var allowed = price.Reserve([new TierDemand(3, 10)], Usage(70), Today);

        var reason = Assert.IsType<CheckResult.Denied>(denied).Cause;
        Assert.IsType<CapacityDenied.TotalExhausted>(reason);
        Assert.True(allowed.IsAllowed);
        Assert.Equal(30, price.Reserved);
    }

    [Fact]
    public void Event_reserve_respects_tier_quota_including_sold_and_reserved()
    {
        var price = Event(null, 0, 0, Tier(3, 20, reserved: 3));

        var denied = price.Reserve([new TierDemand(3, 8)], Usage(10, (3, 10)), Today);

        var reason = Assert.IsType<CapacityDenied.TierExhausted>(Assert.IsType<CheckResult.Denied>(denied).Cause);
        Assert.Equal(3, reason.TierId);
        Assert.Equal(7, reason.Remaining);
        Assert.Equal(0, price.Reserved);
        Assert.Equal(3, price.Categories[0].Reserved);

        Assert.True(price.Reserve([new TierDemand(3, 7)], Usage(10, (3, 10)), Today).IsAllowed);
        Assert.Equal(10, price.Categories[0].Reserved);
    }

    [Fact]
    public void Conversions_bypass_tier_quota_but_count_towards_the_total()
    {
        var price = Event(10, 0, 0, Tier(3, 1));

        var viaTier = price.Reserve([new TierDemand(3, 5, IsConversion: true)], Usage(0, (3, 1)), Today);
        Assert.True(viaTier.IsAllowed);
        Assert.Equal(5, price.Reserved);
        Assert.Equal(0, price.Categories[0].Reserved);

        var overTotal = price.Reserve([new TierDemand(3, 6, IsConversion: true)], Usage(0), Today);
        Assert.IsType<CapacityDenied.TotalExhausted>(Assert.IsType<CheckResult.Denied>(overTotal).Cause);
        Assert.Equal(5, price.Reserved);
    }

    [Fact]
    public void Conversions_do_not_need_a_matching_category()
    {
        var price = Event(null, 0, 0, Tier(3, 5));

        Assert.True(price.Reserve([new TierDemand(99, 1, IsConversion: true)], CapacityUsage.None, Today).IsAllowed);
        Assert.Equal(1, price.Reserved);
    }

    [Fact]
    public void Unknown_tier_denies_with_TierUnavailable()
    {
        var price = Event(null, 0, 0, Tier(3, null));

        var denied = price.Reserve([new TierDemand(4, 1)], CapacityUsage.None, Today);

        var reason = Assert.IsType<CapacityDenied.TierUnavailable>(Assert.IsType<CheckResult.Denied>(denied).Cause);
        Assert.Equal(4, reason.TierId);
    }

    [Fact]
    public void Tier_past_its_sale_window_denies_with_TierUnavailable()
    {
        var price = Event(null, 0, 0, Tier(3, null, until: Today.AddDays(-1)));

        var denied = price.Reserve([new TierDemand(3, 1)], CapacityUsage.None, Today);
        Assert.IsType<CapacityDenied.TierUnavailable>(Assert.IsType<CheckResult.Denied>(denied).Cause);

        var onLastDay = Event(null, 0, 0, Tier(3, null, until: Today));
        Assert.True(onLastDay.Reserve([new TierDemand(3, 1)], CapacityUsage.None, Today).IsAllowed);
    }

    [Fact]
    public void Denial_leaves_every_counter_untouched()
    {
        var price = Event(100, reserved: 5, version: 2, Tier(3, 10, reserved: 1), Tier(4, 1, reserved: 0));

        var denied = price.Reserve([new TierDemand(3, 2), new TierDemand(4, 2)], Usage(0), Today);

        Assert.False(denied.IsAllowed);
        Assert.Equal(5, price.Reserved);
        Assert.Equal(1, price.Categories[0].Reserved);
        Assert.Equal(0, price.Categories[1].Reserved);
        Assert.Equal(2, price.Version);
    }

    [Fact]
    public void Empty_or_zero_demand_is_allowed_without_changes()
    {
        var price = Event(0, 0, 0, Tier(3, 0));

        Assert.True(price.Reserve([], CapacityUsage.None, Today).IsAllowed);
        Assert.True(price.Reserve([new TierDemand(3, 0)], CapacityUsage.None, Today).IsAllowed);
        Assert.Equal(0, price.Reserved);
    }

    [Fact]
    public void Event_release_floors_at_zero_and_skips_conversions_on_tiers()
    {
        var price = Event(100, reserved: 3, version: 0, Tier(3, 10, reserved: 2), Tier(4, 10, reserved: 1));

        price.Release([new TierDemand(3, 5), new TierDemand(4, 1, IsConversion: true), new TierDemand(9, 1)]);

        Assert.Equal(0, price.Reserved);
        Assert.Equal(0, price.Categories[0].Reserved);
        Assert.Equal(1, price.Categories[1].Reserved);
    }

    [Fact]
    public void With_copies_keep_reserved_and_version()
    {
        var price = Event(100, reserved: 7, version: 9, Tier(3, 10, reserved: 2));

        foreach (var copy in new[]
                 {
                     price.WithSalesQuota(50),
                     price.WithAdmissionQuota(80),
                     price.WithConversionOnly(true),
                     price.WithCategories(price.Categories)
                 })
        {
            Assert.Equal(7, copy.Reserved);
            Assert.Equal(9, copy.Version);
            Assert.Equal(2, copy.Categories[0].Reserved);
        }
    }

    [Fact]
    public void Create_starts_without_reservations()
    {
        var price = EventPrice.Create(10, 100, 120, [CategoryPrice.Create(TicketCategory.Adult, 25m, 10, tierId: 3)]);

        Assert.Equal(0, price.Reserved);
        Assert.Equal(0, price.Version);
        Assert.Equal(0, price.Categories[0].Reserved);
    }

    [Fact]
    public void RemainingTotal_subtracts_sold_and_reserved_and_floors_at_zero()
    {
        Assert.Equal(30, Event(100, reserved: 20).RemainingTotal(Usage(50)));
        Assert.Equal(0, Event(100, reserved: 60).RemainingTotal(Usage(50)));
        Assert.Null(Event(null, reserved: 60).RemainingTotal(Usage(50)));
    }

    [Fact]
    public void CategoryPrice_remaining_subtracts_sold_and_reserved()
    {
        Assert.Equal(5, Tier(3, 10, reserved: 2).Remaining(3));
        Assert.Equal(0, Tier(3, 10, reserved: 8).Remaining(5));
        Assert.Null(Tier(3, null, reserved: 8).Remaining(5));
        Assert.True(Tier(3, 10).IsOnSale(Today));
        Assert.False(Tier(3, 10, until: Today.AddDays(-1)).IsOnSale(Today));
    }

    [Fact]
    public void Season_reserve_passes_allows_within_quotas_and_increments_counters()
    {
        var season = Season(50, reserved: 5, PassTier(3, 20, reserved: 2), PassTier(4, null));

        var result = season.ReservePasses([new TierDemand(3, 4), new TierDemand(4, 6)], Usage(10, (3, 10)), Today);

        Assert.True(result.IsAllowed);
        Assert.Equal(15, season.Reserved);
        Assert.Equal(6, season.Categories[0].Reserved);
        Assert.Equal(6, season.Categories[1].Reserved);
        Assert.Equal(4, season.Version);
    }

    [Fact]
    public void Season_reserve_passes_respects_total_and_tier_quotas()
    {
        var season = Season(20, reserved: 10, PassTier(3, 5, reserved: 1));

        var overTotal = season.ReservePasses([new TierDemand(3, 3)], Usage(8, (3, 1)), Today);
        Assert.IsType<CapacityDenied.TotalExhausted>(Assert.IsType<CheckResult.Denied>(overTotal).Cause);

        var overTier = season.ReservePasses([new TierDemand(3, 2)], Usage(0, (3, 3)), Today);
        var reason = Assert.IsType<CapacityDenied.TierExhausted>(Assert.IsType<CheckResult.Denied>(overTier).Cause);
        Assert.Equal(1, reason.Remaining);
        Assert.Equal(10, season.Reserved);
        Assert.Equal(1, season.Categories[0].Reserved);
    }

    [Fact]
    public void Season_reserve_passes_denies_unknown_or_closed_tiers()
    {
        var season = Season(null, 0, PassTier(3, null, offered: false), PassTier(4, null, from: Today.AddDays(1)), PassTier(5, null, until: Today.AddDays(-1)));

        foreach (var tierId in new[] { 3, 4, 5, 6 })
        {
            var denied = season.ReservePasses([new TierDemand(tierId, 1)], CapacityUsage.None, Today);
            var reason = Assert.IsType<CapacityDenied.TierUnavailable>(Assert.IsType<CheckResult.Denied>(denied).Cause);
            Assert.Equal(tierId, reason.TierId);
        }
        Assert.Equal(0, season.Reserved);
    }

    [Fact]
    public void Season_release_passes_floors_at_zero()
    {
        var season = Season(50, reserved: 2, PassTier(3, 10, reserved: 1));

        season.ReleasePasses([new TierDemand(3, 5)]);

        Assert.Equal(0, season.Reserved);
        Assert.Equal(0, season.Categories[0].Reserved);
    }

    [Fact]
    public void Season_remaining_total_and_create_defaults()
    {
        Assert.Equal(15, Season(50, reserved: 5).RemainingTotal(Usage(30)));
        Assert.Null(Season(null).RemainingTotal(Usage(30)));

        var created = SeasonPrice.Create(7, 50, [SeasonCategoryPrice.Create(TicketCategory.Adult, 300m, true, 10, 25m, true, null, tierId: 3)]);
        Assert.Equal(0, created.Reserved);
        Assert.Equal(0, created.Version);
        Assert.Equal(0, created.Categories[0].Reserved);
    }

    [Fact]
    public void SeasonCategoryPrice_IsPassOnSale_honours_offered_flag_and_window()
    {
        Assert.True(PassTier(3, null).IsPassOnSale(Today));
        Assert.False(PassTier(3, null, offered: false).IsPassOnSale(Today));
        Assert.True(PassTier(3, null, from: Today, until: Today).IsPassOnSale(Today));
        Assert.False(PassTier(3, null, from: Today.AddDays(1)).IsPassOnSale(Today));
        Assert.False(PassTier(3, null, until: Today.AddDays(-1)).IsPassOnSale(Today));
        Assert.Equal(7, PassTier(3, 10, reserved: 1).RemainingPasses(2));
        Assert.Null(PassTier(3, null).RemainingPasses(2));
    }
}
