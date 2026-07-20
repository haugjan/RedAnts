using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using Xunit;

namespace RedAnts.Domain.Tests.Sales;

public class DisplayNameTests
{
    [Theory]
    [InlineData(TicketCategory.Adult, "Erwachsen")]
    [InlineData(TicketCategory.AdultPromo, "Sonderaktion Erwachsen")]
    [InlineData(TicketCategory.Youth, "Jugend (bis 19)")]
    [InlineData(TicketCategory.YouthPromo, "Sonderaktion Jugend")]
    [InlineData(TicketCategory.Child, "Kind (bis 5)")]
    public void TicketCategory_DisplayName(TicketCategory category, string expected)
    {
        Assert.Equal(expected, category.DisplayName());
    }

    [Theory]
    [InlineData(TicketStatus.Valid, "Gültig")]
    [InlineData(TicketStatus.Cancelled, "Storniert")]
    [InlineData(TicketStatus.Blocked, "Gesperrt")]
    public void TicketStatus_DisplayName(TicketStatus status, string expected)
    {
        Assert.Equal(expected, status.DisplayName());
    }

    [Theory]
    [InlineData(BuyerType.Private, "Privatperson")]
    [InlineData(BuyerType.Company, "Firma")]
    public void BuyerType_DisplayName(BuyerType type, string expected)
    {
        Assert.Equal(expected, type.DisplayName());
    }

    [Theory]
    [InlineData(MemberCategory.RedAnts, "Red Ants")]
    [InlineData(MemberCategory.Block4, "Block 4")]
    public void MemberCategory_DisplayName(MemberCategory category, string expected)
    {
        Assert.Equal(expected, category.DisplayName());
    }
}
