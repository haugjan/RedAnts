using RedAnts.Infrastructure.Ticketing.Sales;
using Xunit;

namespace RedAnts.Infrastructure.Tests.Sales;

public class HelperPasswordTests
{
    [Fact]
    public void Generate_ReturnsNonNullNonEmpty()
    {
        var result = HelperPassword.Generate();
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Generate_StartsWithUppercaseLetter()
    {
        var result = HelperPassword.Generate();
        Assert.True(char.IsUpper(result[0]), $"Expected uppercase start, got: {result}");
    }

    [Fact]
    public void Generate_ContainsNoSpaces()
    {
        var result = HelperPassword.Generate();
        Assert.DoesNotContain(' ', result);
    }

    [Fact]
    public void Generate_ContainsNoHyphens()
    {
        var result = HelperPassword.Generate();
        Assert.DoesNotContain('-', result);
    }

    [Fact]
    public void Generate_ContainsOnlyLetters()
    {
        var result = HelperPassword.Generate();
        Assert.True(result.All(char.IsLetter), $"Non-letter character found in: {result}");
    }

    [Fact]
    public void Generate_HasMinimumUsableLength()
    {
        var result = HelperPassword.Generate();
        Assert.True(result.Length >= 5, $"Expected length ≥ 5, got {result.Length}: {result}");
    }

    [Fact]
    public void Generate_200Calls_NeverNullOrEmpty()
    {
        for (var i = 0; i < 200; i++)
        {
            var result = HelperPassword.Generate();
            Assert.False(string.IsNullOrEmpty(result), $"Call {i} returned null or empty");
        }
    }

    [Fact]
    public void Generate_200Calls_AlwaysStartWithUppercase()
    {
        for (var i = 0; i < 200; i++)
        {
            var result = HelperPassword.Generate();
            Assert.True(char.IsUpper(result[0]), $"Call {i}: expected uppercase start, got: {result}");
        }
    }

    [Fact]
    public void Generate_200Calls_AlwaysOnlyLetters()
    {
        for (var i = 0; i < 200; i++)
        {
            var result = HelperPassword.Generate();
            Assert.True(result.All(char.IsLetter), $"Call {i}: non-letter in: {result}");
        }
    }

    [Fact]
    public void Generate_ProducesVariety_NotAllIdentical()
    {
        var results = Enumerable.Range(0, 50).Select(_ => HelperPassword.Generate()).ToHashSet();
        Assert.True(results.Count > 10, $"Expected variety across 50 calls, got only {results.Count} distinct values");
    }

    [Fact]
    public void Generate_SecondCharIsLowercase()
    {
        for (var i = 0; i < 50; i++)
        {
            var result = HelperPassword.Generate();
            if (result.Length > 1)
                Assert.True(char.IsLower(result[1]), $"Expected lowercase second char in: {result}");
        }
    }
}
