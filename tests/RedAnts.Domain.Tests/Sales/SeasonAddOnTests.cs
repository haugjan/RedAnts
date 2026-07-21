using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using Xunit;

namespace RedAnts.Domain.Tests.Sales;

public class SeasonAddOnTests
{
    // --- Create validation ---

    [Fact]
    public void Create_ZeroSeasonId_Throws()
    {
        Assert.Throws<DomainException>(() => SeasonAddOn.Create(0, "Parking", 10m, true, 0, AddOnScope.PerPass));
    }

    [Fact]
    public void Create_NegativeSeasonId_Throws()
    {
        Assert.Throws<DomainException>(() => SeasonAddOn.Create(-1, "Parking", 10m, true, 0, AddOnScope.PerPass));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankLabel_Throws(string label)
    {
        Assert.Throws<DomainException>(() => SeasonAddOn.Create(1, label, 10m, true, 0, AddOnScope.PerPass));
    }

    [Fact]
    public void Create_NullLabel_Throws()
    {
        Assert.Throws<DomainException>(() => SeasonAddOn.Create(1, null!, 10m, true, 0, AddOnScope.PerPass));
    }

    [Fact]
    public void Create_NegativePrice_Throws()
    {
        Assert.Throws<DomainException>(() => SeasonAddOn.Create(1, "X", -0.01m, true, 0, AddOnScope.PerPass));
    }

    [Fact]
    public void Create_ZeroPrice_Succeeds()
    {
        var addOn = SeasonAddOn.Create(1, "Gratis", 0m, true, 0, AddOnScope.PerOrder);
        Assert.Equal(0m, addOn.Price);
    }

    // --- Create normalisation ---

    [Fact]
    public void Create_TrimsLabel()
    {
        var addOn = SeasonAddOn.Create(1, "  Parking  ", 10m, true, 0, AddOnScope.PerPass);
        Assert.Equal("Parking", addOn.Label);
    }

    [Fact]
    public void Create_RoundsPriceToTwoDecimals()
    {
        var addOn = SeasonAddOn.Create(1, "X", 9.996m, true, 0, AddOnScope.PerPass);
        Assert.Equal(10.00m, addOn.Price);
    }

    [Fact]
    public void Create_PriceUsesBankersRounding()
    {
        var addOn = SeasonAddOn.Create(1, "X", 9.995m, true, 0, AddOnScope.PerPass);
        Assert.Equal(10.00m, addOn.Price);
    }

    [Fact]
    public void Create_StartsWithIdZero()
    {
        var addOn = SeasonAddOn.Create(1, "Parking", 10m, true, 0, AddOnScope.PerPass);
        Assert.Equal(0, addOn.Id);
    }

    [Fact]
    public void Create_WhitespaceInfoBeforePurchase_BecomesNull()
    {
        var addOn = SeasonAddOn.Create(1, "X", 0m, true, 0, AddOnScope.PerPass, infoBeforePurchase: "   ");
        Assert.Null(addOn.InfoBeforePurchase);
    }

    [Fact]
    public void Create_WhitespaceInfoAfterPurchase_BecomesNull()
    {
        var addOn = SeasonAddOn.Create(1, "X", 0m, true, 0, AddOnScope.PerPass, infoAfterPurchase: "  ");
        Assert.Null(addOn.InfoAfterPurchase);
    }

    [Fact]
    public void Create_WhitespaceLongTitle_BecomesNull()
    {
        var addOn = SeasonAddOn.Create(1, "X", 0m, true, 0, AddOnScope.PerPass, longTitle: " ");
        Assert.Null(addOn.LongTitle);
    }

    [Fact]
    public void Create_InfoFieldsAreTrimmed()
    {
        var addOn = SeasonAddOn.Create(1, "X", 0m, true, 0, AddOnScope.PerPass,
            infoBeforePurchase: "  Bitte buchen  ",
            infoAfterPurchase: "  Danke  ",
            longTitle: "  Parkplatz  ");

        Assert.Equal("Bitte buchen", addOn.InfoBeforePurchase);
        Assert.Equal("Danke", addOn.InfoAfterPurchase);
        Assert.Equal("Parkplatz", addOn.LongTitle);
    }

    [Fact]
    public void Create_NullAllowedTierIds_BecomesEmptyList()
    {
        var addOn = SeasonAddOn.Create(1, "X", 0m, true, 0, AddOnScope.PerPass, allowedTierIds: null);
        Assert.Empty(addOn.AllowedTierIds);
    }

    [Fact]
    public void Create_AllowedTierIds_FiltersZeroAndNegative()
    {
        var addOn = SeasonAddOn.Create(1, "X", 0m, true, 0, AddOnScope.PerPass,
            allowedTierIds: [1, 0, -1, 3]);
        Assert.Equal([1, 3], addOn.AllowedTierIds);
    }

    [Fact]
    public void Create_AllowedTierIds_Deduplicates()
    {
        var addOn = SeasonAddOn.Create(1, "X", 0m, true, 0, AddOnScope.PerPass,
            allowedTierIds: [2, 2, 4, 4]);
        Assert.Equal([2, 4], addOn.AllowedTierIds);
    }

    [Fact]
    public void Create_AllowedTierIds_FiltersAndDeduplicates()
    {
        var addOn = SeasonAddOn.Create(1, "X", 0m, true, 0, AddOnScope.PerPass,
            allowedTierIds: [1, 2, 1, 0, -1, 3]);
        Assert.Equal([1, 2, 3], addOn.AllowedTierIds);
    }

    // --- Create stores values ---

    [Fact]
    public void Create_SetsAllProperties()
    {
        var addOn = SeasonAddOn.Create(
            seasonId: 5, "Parking", price: 20m, active: false, sortOrder: 2, AddOnScope.PerOrder,
            "Bitte buchen", "Danke", "Parkplatz", [1, 2]);

        Assert.Equal(5, addOn.SeasonId);
        Assert.Equal("Parking", addOn.Label);
        Assert.Equal(20m, addOn.Price);
        Assert.False(addOn.Active);
        Assert.Equal(2, addOn.SortOrder);
        Assert.Equal(AddOnScope.PerOrder, addOn.Scope);
        Assert.Equal("Bitte buchen", addOn.InfoBeforePurchase);
        Assert.Equal("Danke", addOn.InfoAfterPurchase);
        Assert.Equal("Parkplatz", addOn.LongTitle);
        Assert.Equal([1, 2], addOn.AllowedTierIds);
    }

    // --- FromPersistence ---

    [Fact]
    public void FromPersistence_SetsId()
    {
        var addOn = SeasonAddOn.FromPersistence(99, 1, "Parking", 10m, true, 0, AddOnScope.PerPass);
        Assert.Equal(99, addOn.Id);
    }

    [Fact]
    public void FromPersistence_NullAllowedTierIds_BecomesEmpty()
    {
        var addOn = SeasonAddOn.FromPersistence(1, 1, "X", 0m, true, 0, AddOnScope.PerPass, allowedTierIds: null);
        Assert.Empty(addOn.AllowedTierIds);
    }
}
