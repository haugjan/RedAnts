using Xunit;
namespace RedAnts.Kernel.Tests;

public class MoneyTests
{
    [Fact]
    public void Of_rounds_to_rappen()
    {
        Assert.Equal(12.35m, Money.Of(12.345m).Amount);
        Assert.Equal(Money.Chf, Money.Of(1m).Currency);
    }

    [Fact]
    public void Of_rejects_negative_amounts_with_the_field_name()
    {
        var ex = Assert.Throws<ValidationException>(() => Money.Of(-1m, field: "price", label: "Preis"));
        Assert.Equal("price", ex.Field);
    }

    [Fact]
    public void Arithmetic_keeps_the_currency()
    {
        var sum = Money.Of(10m) + Money.Of(2.5m);
        Assert.Equal(Money.Of(12.5m), sum);
        Assert.Equal(Money.Of(7.5m), sum - Money.Of(5m));
        Assert.Equal(Money.Of(25m), Money.Of(12.5m).Times(2));
        Assert.True(Money.Zero.IsZero);
    }

    [Fact]
    public void Mixing_currencies_is_a_domain_error() =>
        Assert.Throws<DomainException>(() => Money.Of(1m) + Money.Of(1m, "EUR"));

    [Fact]
    public void ToString_shows_currency_and_two_decimals() =>
        Assert.Equal("CHF 12.50", Money.Of(12.5m).ToString());
}
