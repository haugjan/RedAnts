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
    [InlineData("fan+saison@redants.ch")]
    [InlineData("fan.der.ants@sub.redants.ch")]
    [InlineData("f_a-n@red-ants.co")]
    [InlineData("tickets2026@redants.ch")]
    public void Create_accepts_common_formats(string raw) =>
        Assert.Equal(raw, EmailAddress.Create(raw).Value);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("fan")]
    [InlineData("fan@redants")]
    [InlineData("fan redants@x.ch")]
    [InlineData("@redants.ch")]
    [InlineData("fan@.ch")]
    [InlineData("fan@redants..ch")]
    [InlineData("fan..ants@redants.ch")]
    [InlineData("fan@redants.ch.")]
    public void Create_rejects_blank_or_malformed_input(string? raw)
    {
        var ex = Assert.Throws<ValidationException>(() => EmailAddress.Create(raw, field: "mail", label: "Mail"));
        Assert.Equal("mail", ex.Field);
    }

    [Theory]
    [InlineData("jörg@redants.ch")]
    [InlineData("müller@redants.ch")]
    [InlineData("fan@rotamöisen.ch")]
    public void Create_rejects_umlauts(string raw) =>
        Assert.Throws<ValidationException>(() => EmailAddress.Create(raw));

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
        Assert.False(EmailAddress.IsValid("nope"));
        Assert.True(EmailAddress.IsValid("ok@redants.ch"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Optional_treats_blank_as_no_address(string? raw) =>
        Assert.Null(EmailAddress.Optional(raw));

    [Fact]
    public void Optional_still_rejects_a_malformed_address()
    {
        var ex = Assert.Throws<ValidationException>(() => EmailAddress.Optional("nope", field: "holderEmail"));
        Assert.Equal("holderEmail", ex.Field);
    }

    [Fact]
    public void HasDomain_ignores_case_and_leading_at()
    {
        var email = EmailAddress.Create("Fan@RedAnts.ch");
        Assert.True(email.HasDomain("redants.ch"));
        Assert.True(email.HasDomain("@redants.ch"));
        Assert.False(email.HasDomain("ants.ch"));
    }

    [Fact]
    public void The_default_value_carries_no_address() =>
        Assert.Equal("", default(EmailAddress).Value);
}
