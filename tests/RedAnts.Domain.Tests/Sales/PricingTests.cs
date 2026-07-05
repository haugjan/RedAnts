using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using Xunit;

namespace RedAnts.Domain.Tests.Sales;

public class PricingTests
{
    [Fact]
    public void CategoryPrice_Create_RoundsPriceToTwoDecimals()
    {
        var price = CategoryPrice.Create(TicketCategory.Adult, 12.349m, quota: 100);

        Assert.Equal(12.35m, price.SalePrice);
        Assert.Equal(100, price.Quota);
    }

    [Fact]
    public void CategoryPrice_Create_UsesBankersRoundingAtMidpoint()
    {
        Assert.Equal(12.34m, CategoryPrice.Create(TicketCategory.Adult, 12.345m, null).SalePrice);
        Assert.Equal(12.36m, CategoryPrice.Create(TicketCategory.Adult, 12.355m, null).SalePrice);
    }

    [Fact]
    public void CategoryPrice_Create_AllowsNullQuota()
    {
        var price = CategoryPrice.Create(TicketCategory.Adult, 10m, quota: null);
        Assert.Null(price.Quota);
    }

    [Fact]
    public void CategoryPrice_Create_RejectsNegativePriceOrQuota()
    {
        Assert.Throws<DomainException>(() => CategoryPrice.Create(TicketCategory.Adult, -0.01m, null));
        Assert.Throws<DomainException>(() => CategoryPrice.Create(TicketCategory.Adult, 10m, -1));
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
        var price = EventPrice.Create(1, 500, 400, null!);
        Assert.Empty(price.Categories);
    }

    [Fact]
    public void SeasonPrice_Create_RejectsInvalidSeasonOrNegativeQuota()
    {
        Assert.Throws<DomainException>(() => SeasonPrice.Create(0, null, []));
        Assert.Throws<DomainException>(() => SeasonPrice.Create(1, -1, []));
    }
}
