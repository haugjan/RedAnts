using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using Xunit;

namespace RedAnts.Domain.Tests.Sales;

public class BuyerTests
{
    [Fact]
    public void Create_Private_KeepsNames_DropsCompany()
    {
        var buyer = Buyer.Create(BuyerType.Private, "Anna", "Muster", "IgnoredCo");

        Assert.Equal(BuyerType.Private, buyer.Type);
        Assert.Equal("Anna", buyer.FirstName);
        Assert.Equal("Muster", buyer.LastName);
        Assert.Null(buyer.Company);
        Assert.Equal("Anna Muster", buyer.DisplayName);
    }

    [Fact]
    public void Create_Company_KeepsCompany_DropsNames()
    {
        var buyer = Buyer.Create(BuyerType.Company, "Anna", "Muster", "Acme AG");

        Assert.Equal(BuyerType.Company, buyer.Type);
        Assert.Null(buyer.FirstName);
        Assert.Null(buyer.LastName);
        Assert.Equal("Acme AG", buyer.Company);
        Assert.Equal("Acme AG", buyer.DisplayName);
    }

    [Fact]
    public void Create_Private_RequiresBothNames()
    {
        Assert.Throws<DomainException>(() => Buyer.Create(BuyerType.Private, "Anna", "  ", null));
        Assert.Throws<DomainException>(() => Buyer.Create(BuyerType.Private, null, "Muster", null));
    }

    [Fact]
    public void Create_Company_RequiresCompanyName()
    {
        Assert.Throws<DomainException>(() => Buyer.Create(BuyerType.Company, "Anna", "Muster", "   "));
    }

    [Fact]
    public void FromPersistence_AllEmpty_ReturnsNull()
    {
        Assert.Null(Buyer.FromPersistence((int)BuyerType.Private, "  ", null, ""));
    }

    [Fact]
    public void FromPersistence_WithData_Rehydrates()
    {
        var buyer = Buyer.FromPersistence((int)BuyerType.Company, null, null, "Acme AG");

        Assert.NotNull(buyer);
        Assert.Equal("Acme AG", buyer!.Company);
    }
}
