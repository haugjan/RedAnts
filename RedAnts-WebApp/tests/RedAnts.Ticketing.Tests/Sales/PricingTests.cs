using RedAnts.Ticketing.Domain;
using RedAnts.Ticketing.Domain.Sales;
using Xunit;

namespace RedAnts.Ticketing.Tests.Sales;

public class PricingTests
{
    [Fact]
    public void CategoryPrice_Create_SnapsThePriceToFiveRappen()
    {
        var price = CategoryPrice.Create(TicketCategory.Adult, Money.Stored(12.349m), quota: 100);

        Assert.Equal(12.35m, price.SalePrice.Amount);
        Assert.Equal(100, price.Quota);
    }

    [Fact]
    public void CategoryPrice_Create_RoundsAwayFromZeroAtTheMidpoint()
    {
        Assert.Equal(12.35m, CategoryPrice.Create(TicketCategory.Adult, Money.Stored(12.325m), null).SalePrice.Amount);
        Assert.Equal(12.30m, CategoryPrice.Create(TicketCategory.Adult, Money.Stored(12.32m), null).SalePrice.Amount);
    }

    [Fact]
    public void CategoryPrice_Create_AllowsNullQuota()
    {
        var price = CategoryPrice.Create(TicketCategory.Adult, Money.Chf(10m), quota: null);
        Assert.Null(price.Quota);
    }

    [Fact]
    public void CategoryPrice_Create_RejectsNegativePriceOrQuota()
    {
        Assert.Throws<ValidationException>(() => CategoryPrice.Create(TicketCategory.Adult, Money.Stored(-0.01m), null));
        Assert.Throws<DomainException>(() => CategoryPrice.Create(TicketCategory.Adult, Money.Chf(10m), -1));
    }

    [Fact]
    public void EventPrice_Create_RejectsInvalidEventId()
    {
        Assert.Throws<DomainException>(() => EventPrice.Create(0, null, null, []));
    }

    [Fact]
    public void EventPrice_Create_RejectsNegativeQuotas()
    {
        Assert.Throws<DomainException>(() => EventPrice.Create(1, totalSalesQuota: -1, admissionQuota: null, []));
        Assert.Throws<DomainException>(() => EventPrice.Create(1, totalSalesQuota: null, admissionQuota: -1, []));
    }

    [Fact]
    public void EventPrice_Create_NullCategories_BecomesEmptyList()
    {
        var price = EventPrice.Create(1, 400, 500, null!);
        Assert.Empty(price.Categories);
    }

    [Fact]
    public void SeasonPrice_Create_RejectsInvalidSeasonOrNegativeQuota()
    {
        Assert.Throws<DomainException>(() => SeasonPrice.Create(0, null, []));
        Assert.Throws<DomainException>(() => SeasonPrice.Create(1, -1, []));
    }
}
