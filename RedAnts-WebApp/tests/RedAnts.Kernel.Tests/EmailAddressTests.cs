using Xunit;
namespace RedAnts.Kernel.Tests;

public class EmailAddressTests
{
    [Fact]
    public void Create_trims_and_keeps_the_address()
    {
        var email = EmailAddress.Create("  fan@redants.ch ");
        Assert.Equal("fan@redants.ch", email.Value);
        Assert.Equal("fan@redants.ch", email.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("fan")]
    [InlineData("fan@redants")]
    [InlineData("fan redants@x.ch")]
    public void Create_rejects_blank_or_malformed_input(string? raw)
    {
        var ex = Assert.Throws<ValidationException>(() => EmailAddress.Create(raw, field: "mail", label: "Mail"));
        Assert.Equal("mail", ex.Field);
    }

    [Fact]
    public void Create_rejects_overlong_addresses()
    {
        var raw = new string('a', EmailAddress.MaxLength) + "@redants.ch";
        Assert.Throws<ValidationException>(() => EmailAddress.Create(raw));
    }

    [Fact]
    public void TryCreate_returns_null_instead_of_throwing()
    {
        Assert.Null(EmailAddress.TryCreate("nope"));
        Assert.NotNull(EmailAddress.TryCreate("ok@redants.ch"));
    }

    [Fact]
    public void HasDomain_ignores_case_and_leading_at()
    {
        var email = EmailAddress.Create("Fan@RedAnts.ch");
        Assert.True(email.HasDomain("redants.ch"));
        Assert.True(email.HasDomain("@redants.ch"));
        Assert.False(email.HasDomain("ants.ch"));
    }
}
