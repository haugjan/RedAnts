using System.Globalization;
using RedAnts.Features.Ticketing;
using Xunit;

namespace RedAnts.Features.Tests;

public class MoneyFormatTests
{
    public MoneyFormatTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("de-CH");
    }

    [Fact]
    public void Amount_WholeNumber_OmitsDecimals()
    {
        Assert.Equal("10", MoneyFormat.Amount(10m));
    }

    [Fact]
    public void Amount_Zero_ReturnsZero()
    {
        Assert.Equal("0", MoneyFormat.Amount(0m));
    }

    [Fact]
    public void Amount_WithDecimals_ShowsTwoDecimalPlaces()
    {
        Assert.Equal("10.50", MoneyFormat.Amount(10.50m));
    }

    [Fact]
    public void Amount_SingleDecimalDigit_PadsToTwo()
    {
        Assert.Equal("5.10", MoneyFormat.Amount(5.1m));
    }

    [Fact]
    public void Amount_SmallDecimal_ShowsTwoPlaces()
    {
        Assert.Equal("0.10", MoneyFormat.Amount(0.10m));
        Assert.Equal("0.05", MoneyFormat.Amount(0.05m));
    }

    [Fact]
    public void Amount_RoundsDownCorrectly()
    {
        Assert.Equal("10.99", MoneyFormat.Amount(10.994m));
    }

    [Fact]
    public void Amount_RoundsUpCorrectly()
    {
        Assert.Equal("10.99", MoneyFormat.Amount(10.989m));
    }

    [Fact]
    public void Amount_AfterRoundingBecomesWhole_OmitsDecimals()
    {
        Assert.Equal("11", MoneyFormat.Amount(10.999m));
    }

    [Fact]
    public void Amount_LargeWholeNumber_UsesSwissThousandsSeparator()
    {
        var sep = CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator;
        Assert.Equal($"1{sep}000", MoneyFormat.Amount(1000m));
    }

    [Fact]
    public void Amount_VeryLargeWhole_MultipleThousandsSeparators()
    {
        var sep = CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator;
        Assert.Equal($"10{sep}000", MoneyFormat.Amount(10000m));
    }

    [Fact]
    public void Amount_LargeDecimal_UsesThousandsSeparatorAndDecimals()
    {
        var sep = CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator;
        Assert.Equal($"1{sep}234.50", MoneyFormat.Amount(1234.5m));
    }

    [Fact]
    public void Amount_UsesBankersRoundingAtMidpoint_RoundsToEven()
    {
        Assert.Equal("10.34", MoneyFormat.Amount(10.345m));
        Assert.Equal("10.36", MoneyFormat.Amount(10.355m));
    }

    [Theory]
    [InlineData(20.00)]
    [InlineData(100.00)]
    [InlineData(0.00)]
    [InlineData(1000.00)]
    public void Amount_ExactlyWholeDecimal_NeverContainsDot(double input)
    {
        var result = MoneyFormat.Amount((decimal)input);
        Assert.DoesNotContain(".", result);
    }

    [Theory]
    [InlineData(10.50)]
    [InlineData(0.99)]
    [InlineData(123.45)]
    public void Amount_NonWholeDecimal_ContainsDot(double input)
    {
        var result = MoneyFormat.Amount((decimal)input);
        Assert.Contains(".", result);
    }
}
