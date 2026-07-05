using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using Xunit;

namespace RedAnts.Domain.Tests;

public class BillingAddressTests
{
    [Fact]
    public void Create_Private_RequiresFirstAndLastName()
    {
        Assert.Throws<DomainException>(() => BillingAddress.Create(
            BuyerType.Private, "", "Muster", null, "Street 1", null, "8400", "Winterthur", "CH", "a@b.ch", null));
        Assert.Throws<DomainException>(() => BillingAddress.Create(
            BuyerType.Private, "Anna", "", null, "Street 1", null, "8400", "Winterthur", "CH", "a@b.ch", null));
    }

    [Fact]
    public void Create_Company_RequiresCompanyName_NotPersonName()
    {
        var address = BillingAddress.Create(
            BuyerType.Company, null, null, "Acme AG", "Street 1", null, "8400", "Winterthur", "CH", "a@b.ch", null);

        Assert.Equal("Acme AG", address.Company);
        Assert.Equal("Acme AG", address.FullName);
    }

    [Fact]
    public void Create_Company_WithoutCompanyName_Throws()
    {
        Assert.Throws<DomainException>(() => BillingAddress.Create(
            BuyerType.Company, "Anna", "Muster", "   ", "Street 1", null, "8400", "Winterthur", "CH", "a@b.ch", null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-at-sign")]
    public void Create_RejectsInvalidEmail(string email)
    {
        Assert.Throws<DomainException>(() => BillingAddress.Create(
            BuyerType.Private, "Anna", "Muster", null, "Street 1", null, "8400", "Winterthur", "CH", email, null));
    }

    [Fact]
    public void Create_RequiresStreetAndCity()
    {
        Assert.Throws<DomainException>(() => BillingAddress.Create(
            BuyerType.Private, "Anna", "Muster", null, " ", null, "8400", "Winterthur", "CH", "a@b.ch", null));
        Assert.Throws<DomainException>(() => BillingAddress.Create(
            BuyerType.Private, "Anna", "Muster", null, "Street 1", null, "8400", " ", "CH", "a@b.ch", null));
    }

    [Fact]
    public void Create_DefaultsCountryToSchweiz_AndValidatesSwissPostalCode()
    {
        var address = BillingAddress.Create(
            BuyerType.Private, "Anna", "Muster", null, "Street 1", null, "8400", "Winterthur", null, "a@b.ch", null);

        Assert.Equal("Schweiz", address.Country);
        Assert.Equal("8400", address.PostalCode.Value);
    }

    [Fact]
    public void Create_TrimsFieldsAndNullsEmptyOptionals()
    {
        var address = BillingAddress.Create(
            BuyerType.Private, "  Anna ", " Muster ", "  ", "  Street 1 ", "   ", "8400", " Winterthur ", "CH",
            "  a@b.ch ", "   ");

        Assert.Equal("Anna", address.FirstName);
        Assert.Equal("Muster", address.LastName);
        Assert.Null(address.Company);
        Assert.Equal("Street 1", address.Street);
        Assert.Null(address.AddressLine2);
        Assert.Equal("a@b.ch", address.Email);
        Assert.Null(address.Phone);
    }

    [Fact]
    public void FullName_ForPrivate_JoinsFirstAndLast()
    {
        var address = BillingAddress.Create(
            BuyerType.Private, "Anna", "Muster", null, "Street 1", null, "8400", "Winterthur", "CH", "a@b.ch", null);

        Assert.Equal("Anna Muster", address.FullName);
    }

    [Fact]
    public void ToBuyer_CopiesTypeAndName()
    {
        var address = BillingAddress.Create(
            BuyerType.Company, null, null, "Acme AG", "Street 1", null, "8400", "Winterthur", "CH", "a@b.ch", null);

        var buyer = address.ToBuyer();

        Assert.Equal(BuyerType.Company, buyer.Type);
        Assert.Equal("Acme AG", buyer.DisplayName);
    }
}
