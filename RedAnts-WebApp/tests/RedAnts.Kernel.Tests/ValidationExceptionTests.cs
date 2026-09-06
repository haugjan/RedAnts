using Xunit;
namespace RedAnts.Kernel.Tests;

public class ValidationExceptionTests
{
    [Fact]
    public void ThrowIfBlank_names_field_and_label()
    {
        var ex = Assert.Throws<ValidationException>(() => ValidationException.ThrowIfBlank(" ", "name", "Name"));
        Assert.Equal("name", ex.Field);
        Assert.Equal("Name ist Pflicht.", ex.Message);
    }

    [Fact]
    public void ThrowIfBlank_accepts_text() =>
        ValidationException.ThrowIfBlank("x", "name", "Name");

    [Fact]
    public void ThrowIfLongerThan_mentions_the_limit()
    {
        var ex = Assert.Throws<ValidationException>(() => ValidationException.ThrowIfLongerThan("abcd", 3, "code", "Code"));
        Assert.Contains("3", ex.Message);
    }

    [Fact]
    public void ThrowIfNegative_allows_zero()
    {
        ValidationException.ThrowIfNegative(0m, "amount", "Betrag");
        Assert.Throws<ValidationException>(() => ValidationException.ThrowIfNegative(-0.01m, "amount", "Betrag"));
    }
}
