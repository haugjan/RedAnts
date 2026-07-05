using RedAnts.Domain.Ticketing;
using Xunit;

namespace RedAnts.Domain.Tests;

public class PostalCodeTests
{
    [Theory]
    [InlineData("8400")]
    [InlineData("1000")]
    [InlineData("9999")]
    public void Create_AcceptsValidSwissCode(string value)
    {
        var code = PostalCode.Create(value, "Schweiz");
        Assert.Equal(value, code.Value);
    }

    [Fact]
    public void Create_TrimsWhitespace()
    {
        Assert.Equal("8400", PostalCode.Create("  8400  ", "CH").Value);
    }

    [Theory]
    [InlineData("840")]
    [InlineData("84000")]
    [InlineData("0999")]
    [InlineData("84a0")]
    public void Create_RejectsInvalidSwissCode(string value)
    {
        Assert.Throws<DomainException>(() => PostalCode.Create(value, "Schweiz"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Switzerland")]
    [InlineData("CH")]
    [InlineData("")]
    public void Create_TreatsMissingOrSwissCountryAsSwiss(string? country)
    {
        Assert.Throws<DomainException>(() => PostalCode.Create("12", country));
    }

    [Fact]
    public void Create_ForeignCode_AllowsNonNumericUpToTenChars()
    {
        var code = PostalCode.Create("W1A 0AX", "United Kingdom");
        Assert.Equal("W1A 0AX", code.Value);
    }

    [Fact]
    public void Create_ForeignCode_RejectsOverTenChars()
    {
        Assert.Throws<DomainException>(() => PostalCode.Create("12345678901", "Germany"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsBlank(string value)
    {
        Assert.Throws<DomainException>(() => PostalCode.Create(value, "Germany"));
    }

    [Fact]
    public void FromPersistence_DoesNotValidate()
    {
        var code = PostalCode.FromPersistence("  invalid-pc  ");
        Assert.Equal("invalid-pc", code.Value);
    }
}
