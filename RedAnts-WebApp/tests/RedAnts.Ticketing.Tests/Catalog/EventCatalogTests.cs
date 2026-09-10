using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Catalog;
using RedAnts.Ticketing.Features.Catalog.Pricing;
using Xunit;

namespace RedAnts.Ticketing.Tests.Catalog;

public class EventCatalogTests
{
    private const int EventId = 42;

    private static EventPrice ExistingPrice() => EventPrice.FromPersistence(7, EventId, 100, 200,
        [CategoryPrice.FromPersistence(TicketCategory.Adult, 25m, 10, tierId: 3)], conversionOnly: true, reserved: 4, version: 2);

    [Fact]
    public async Task Admission_quota_creates_a_price_row_when_none_exists()
    {
        var prices = new RecordingEventPrices();

        await new SetEventAdmissionQuota.Handler(prices).HandleAsync(new SetEventAdmissionQuota.Command(EventId, 150));

        var saved = Assert.Single(prices.Saved);
        Assert.Equal(EventId, saved.EventId);
        Assert.Equal(150, saved.AdmissionQuota);
        Assert.Null(saved.TotalSalesQuota);
        Assert.Empty(saved.Categories);
    }

    [Fact]
    public async Task Admission_quota_keeps_conversion_only_categories_and_reservations()
    {
        var prices = new RecordingEventPrices();
        prices.Seed(ExistingPrice());

        await new SetEventAdmissionQuota.Handler(prices).HandleAsync(new SetEventAdmissionQuota.Command(EventId, 300));

        var saved = Assert.Single(prices.Saved);
        Assert.Equal(7, saved.Id);
        Assert.Equal(300, saved.AdmissionQuota);
        Assert.Equal(100, saved.TotalSalesQuota);
        Assert.True(saved.ConversionOnly);
        Assert.Single(saved.Categories);
        Assert.Equal(4, saved.Reserved);
        Assert.Equal(2, saved.Version);
    }

    [Fact]
    public async Task Sales_quota_creates_a_price_row_when_none_exists()
    {
        var prices = new RecordingEventPrices();

        await new SetEventSalesQuota.Handler(prices).HandleAsync(new SetEventSalesQuota.Command(EventId, 80));

        var saved = Assert.Single(prices.Saved);
        Assert.Equal(80, saved.TotalSalesQuota);
        Assert.Null(saved.AdmissionQuota);
    }

    [Fact]
    public async Task Sales_quota_keeps_the_rest_of_the_price()
    {
        var prices = new RecordingEventPrices();
        prices.Seed(ExistingPrice());

        await new SetEventSalesQuota.Handler(prices).HandleAsync(new SetEventSalesQuota.Command(EventId, null));

        var saved = Assert.Single(prices.Saved);
        Assert.Null(saved.TotalSalesQuota);
        Assert.Equal(200, saved.AdmissionQuota);
        Assert.True(saved.ConversionOnly);
        Assert.Single(saved.Categories);
    }

    [Fact]
    public async Task Sales_quota_above_the_admission_quota_is_rejected_and_nothing_is_saved()
    {
        var prices = new RecordingEventPrices();
        prices.Seed(ExistingPrice());

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            new SetEventSalesQuota.Handler(prices).HandleAsync(new SetEventSalesQuota.Command(EventId, 250)));

        Assert.Equal(EventPrice.SalesAboveAdmission, ex.Message);
        Assert.Empty(prices.Saved);
    }

    [Fact]
    public async Task Admission_quota_below_the_sales_quota_is_rejected()
    {
        var prices = new RecordingEventPrices();
        prices.Seed(ExistingPrice());

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            new SetEventAdmissionQuota.Handler(prices).HandleAsync(new SetEventAdmissionQuota.Command(EventId, 50)));

        Assert.Equal(EventPrice.SalesAboveAdmission, ex.Message);
        Assert.Empty(prices.Saved);
    }

    [Fact]
    public async Task Pricing_replaces_the_categories_and_keeps_the_quotas()
    {
        var prices = new RecordingEventPrices();
        prices.Seed(ExistingPrice());

        await new SetEventPricing.Handler(prices).HandleAsync(new SetEventPricing.Command(EventId,
        [
            new EventCategoryPriceInput(3, 30.126m, 12, new DateOnly(2026, 12, 31)),
            new EventCategoryPriceInput(5, 15m, null, null)
        ]));

        var saved = Assert.Single(prices.Saved);
        Assert.Equal(100, saved.TotalSalesQuota);
        Assert.Equal(200, saved.AdmissionQuota);
        Assert.True(saved.ConversionOnly);
        Assert.Equal(2, saved.Categories.Count);
        var adult = saved.Categories.Single(c => c.TierId == 3);
        Assert.Equal(30.15m, adult.SalePrice.Amount);
        Assert.Equal(12, adult.Quota);
        Assert.Equal(new DateOnly(2026, 12, 31), adult.AvailableUntil);
        Assert.Null(saved.Categories.Single(c => c.TierId == 5).Quota);
    }

    [Fact]
    public async Task Pricing_creates_a_price_row_when_none_exists()
    {
        var prices = new RecordingEventPrices();

        await new SetEventPricing.Handler(prices).HandleAsync(new SetEventPricing.Command(EventId,
            [new EventCategoryPriceInput(3, 20m, null, null)]));

        var saved = Assert.Single(prices.Saved);
        Assert.Equal(0, saved.Id);
        Assert.Null(saved.TotalSalesQuota);
        Assert.Null(saved.AdmissionQuota);
        Assert.Single(saved.Categories);
    }

    [Fact]
    public async Task Pricing_rejects_a_negative_price_before_saving()
    {
        var prices = new RecordingEventPrices();
        prices.Seed(ExistingPrice());

        await Assert.ThrowsAsync<ValidationException>(() =>
            new SetEventPricing.Handler(prices).HandleAsync(new SetEventPricing.Command(EventId,
                [new EventCategoryPriceInput(3, -1m, null, null)])));

        Assert.Empty(prices.Saved);
    }

    [Fact]
    public async Task Conversion_rules_write_the_three_card_rules_and_the_flag()
    {
        var rules = new RecordingConversionRules();

        await new SetEventConversionRules.Handler(rules).HandleAsync(
            new SetEventConversionRules.Command(EventId, SeasonPassRequired: true, MemberCardRequired: false, FlexDiscount: 5.555m, ConversionOnly: true));

        Assert.Equal(3, rules.Rules.Count);
        Assert.Contains((EventId, TicketType.SeasonPass, (decimal?)0m), rules.Rules);
        Assert.Contains((EventId, TicketType.MemberCard, (decimal?)null), rules.Rules);
        Assert.Contains((EventId, TicketType.SeasonSingle, (decimal?)5.56m), rules.Rules);
        Assert.True(rules.ConversionOnly[EventId]);
    }

    [Fact]
    public async Task Conversion_rules_clamp_a_negative_flex_discount_to_zero()
    {
        var rules = new RecordingConversionRules();

        await new SetEventConversionRules.Handler(rules).HandleAsync(
            new SetEventConversionRules.Command(EventId, false, false, -3m, false));

        Assert.Contains((EventId, TicketType.SeasonSingle, (decimal?)0m), rules.Rules);
        Assert.False(rules.ConversionOnly[EventId]);
    }

    [Fact]
    public async Task Free_entry_quotas_are_saved_per_type()
    {
        var freeEntries = new RecordingFreeEntryQuotas();

        await new SetEventFreeEntryQuotas.Handler(freeEntries).HandleAsync(new SetEventFreeEntryQuotas.Command(EventId,
            new Dictionary<FreeEntryType, int?> { [FreeEntryType.Player] = 20, [FreeEntryType.Staff] = null },
            new Dictionary<FreeEntryType, int?> { [FreeEntryType.Player] = 3, [FreeEntryType.Official] = 0 }));

        var quota = freeEntries.Saved[EventId];
        Assert.Equal(20, quota.QuotaFor(FreeEntryType.Player));
        Assert.Null(quota.QuotaFor(FreeEntryType.Staff));
        Assert.Equal(3, quota.FixedFor(FreeEntryType.Player));
        Assert.Equal(0, quota.FixedFor(FreeEntryType.Official));
        Assert.Equal(3, quota.FixedTotal);
    }

    [Fact]
    public async Task Free_entry_quotas_reject_negative_values()
    {
        var freeEntries = new RecordingFreeEntryQuotas();

        await Assert.ThrowsAsync<DomainException>(() =>
            new SetEventFreeEntryQuotas.Handler(freeEntries).HandleAsync(new SetEventFreeEntryQuotas.Command(EventId,
                new Dictionary<FreeEntryType, int?> { [FreeEntryType.Player] = -1 },
                new Dictionary<FreeEntryType, int?>())));

        Assert.Empty(freeEntries.Saved);
    }

    [Fact]
    public async Task Sales_status_is_passed_to_the_content_writer()
    {
        var status = new RecordingEventStatus();

        await new SetEventSalesStatus.Handler(status).HandleAsync(new SetEventSalesStatus.Command(EventId, EventStatus.Intern));

        Assert.Equal([(EventId, EventStatus.Intern)], status.Changes);
    }
}
