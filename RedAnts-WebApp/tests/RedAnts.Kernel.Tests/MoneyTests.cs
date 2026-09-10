using System.Globalization;
using Xunit;
namespace RedAnts.Kernel.Tests;

public class MoneyTests
{
    [Fact]
    public void Of_rounds_to_rappen()
    {
        Assert.Equal(12.35m, Money.Of(12.345m).Amount);
        Assert.Equal(12.32m, Money.Of(12.324m).Amount);
        Assert.Equal(Money.SwissFranc, Money.Of(1m).Currency);
    }

    [Theory]
    [InlineData("12.345", "12.35")]
    [InlineData("12.32", "12.30")]
    [InlineData("12.325", "12.35")]
    [InlineData("12.30", "12.30")]
    [InlineData("20", "20")]
    [InlineData("0.02", "0")]
    [InlineData("0", "0")]
    public void Chf_rounds_to_five_rappen(string raw, string expected)
    {
        var money = Money.Chf(Amount(raw));
        Assert.Equal(Amount(expected), money.Amount);
        Assert.Equal(Money.SwissFranc, money.Currency);
    }

    private static decimal Amount(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

    [Fact]
    public void Chf_leaves_prices_that_already_sit_on_the_grid()
    {
        foreach (var price in new[] { 5m, 10m, 12.50m, 15m, 20m, 25m, 30m, 100m })
            Assert.Equal(price, Money.Chf(price).Amount);
    }

    [Fact]
    public void Stored_keeps_the_raw_decimal_of_the_database() =>
        Assert.Equal(12.345m, Money.Stored(12.345m).Amount);

    [Fact]
    public void Of_rejects_negative_amounts_with_the_field_name()
    {
        var ex = Assert.Throws<ValidationException>(() => Money.Of(-1m, field: "price", label: "Preis"));
        Assert.Equal("price", ex.Field);
        Assert.Throws<ValidationException>(() => Money.Chf(-0.05m));
    }

    [Fact]
    public void Arithmetic_keeps_the_currency()
    {
        var sum = Money.Of(10m) + Money.Of(2.5m);
        Assert.Equal(Money.Of(12.5m), sum);
        Assert.Equal(Money.Of(7.5m), sum - Money.Of(5m));
        Assert.Equal(Money.Of(25m), Money.Of(12.5m).Times(2));
        Assert.True(Money.Zero.IsZero);
        Assert.True(Money.Of(1m).IsPositive);
    }

    [Fact]
    public void Times_multiplies_a_line_by_its_quantity() =>
        Assert.Equal(Money.Chf(60m), Money.Chf(12m).Times(5));

    [Fact]
    public void Sum_adds_up_a_list_of_lines()
    {
        Assert.Equal(Money.Chf(45m), Money.Sum([Money.Chf(20m), Money.Chf(15m), Money.Chf(10m)]));
        Assert.Equal(Money.Zero, Money.Sum([]));
    }

    [Fact]
    public void Comparison_orders_amounts_of_the_same_currency()
    {
        Assert.True(Money.Of(20m) > Money.Of(15m));
        Assert.True(Money.Of(15m) < Money.Of(20m));
        Assert.True(Money.Of(20m) >= Money.Of(20m));
        Assert.True(Money.Of(20m) <= Money.Of(20m));
    }

    [Fact]
    public void Mixing_currencies_is_a_domain_error()
    {
        Assert.Throws<DomainException>(() => Money.Of(1m) + Money.Of(1m, "EUR"));
        Assert.Throws<DomainException>(() => Money.Of(1m) > Money.Of(1m, "EUR"));
    }

    [Fact]
    public void The_default_value_is_zero_swiss_francs()
    {
        Assert.Equal(Money.Zero, default(Money));
        Assert.Equal(Money.SwissFranc, default(Money).Currency);
    }

    [Fact]
    public void ToString_shows_currency_and_two_decimals() =>
        Assert.Equal("CHF 12.50", Money.Of(12.5m).ToString());
}
