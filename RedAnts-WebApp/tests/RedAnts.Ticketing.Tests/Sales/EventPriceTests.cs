using Xunit;
using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Ticketing.Tests.Sales;

public class EventPriceTests
{
    private static EventPrice Existing() => EventPrice.FromPersistence(7, 42, 100, 80,
        [CategoryPrice.FromPersistence(TicketCategory.Adult, 25m, 10, tierId: 3)], conversionOnly: true);

    [Fact]
    public void WithSalesQuota_keeps_everything_else()
    {
        var updated = Existing().WithSalesQuota(150);

        Assert.Equal(150, updated.TotalSalesQuota);
        Assert.Equal(7, updated.Id);
        Assert.Equal(42, updated.EventId);
        Assert.Equal(80, updated.AdmissionQuota);
        Assert.True(updated.ConversionOnly);
        Assert.Single(updated.Categories);
    }

    [Fact]
    public void WithAdmissionQuota_keeps_conversion_only()
    {
        var updated = Existing().WithAdmissionQuota(null);

        Assert.Null(updated.AdmissionQuota);
        Assert.Equal(100, updated.TotalSalesQuota);
        Assert.True(updated.ConversionOnly);
    }

    [Fact]
    public void WithConversionOnly_keeps_quotas_and_categories()
    {
        var updated = Existing().WithConversionOnly(false);

        Assert.False(updated.ConversionOnly);
        Assert.Equal(100, updated.TotalSalesQuota);
        Assert.Equal(80, updated.AdmissionQuota);
        Assert.Single(updated.Categories);
    }

    [Fact]
    public void WithCategories_keeps_quotas_and_conversion_only()
    {
        var updated = Existing().WithCategories([]);

        Assert.Empty(updated.Categories);
        Assert.Equal(100, updated.TotalSalesQuota);
        Assert.Equal(80, updated.AdmissionQuota);
        Assert.True(updated.ConversionOnly);
    }

    [Fact]
    public void Negative_quotas_are_rejected()
    {
        Assert.Throws<DomainException>(() => Existing().WithSalesQuota(-1));
        Assert.Throws<DomainException>(() => Existing().WithAdmissionQuota(-1));
    }
}
