using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using Xunit;

namespace RedAnts.Domain.Tests.Sales;

public class SeasonPassAndCardTests
{
    [Fact]
    public void SeasonPass_Create_RoundsPrice_AndStartsValid()
    {
        var pass = SeasonPass.Create(seasonId: 3, TicketCategory.Adult, price: 199.999m, orderId: 1);

        Assert.Equal(200.00m, pass.Price);
        Assert.Equal(TicketStatus.Valid, pass.Status);
        Assert.NotEqual(Guid.Empty, pass.Uuid);
    }

    [Fact]
    public void SeasonPass_Create_RejectsNegativePriceOrSeason()
    {
        Assert.Throws<DomainException>(() => SeasonPass.Create(3, TicketCategory.Adult, -1m, null));
        Assert.Throws<DomainException>(() => SeasonPass.Create(0, TicketCategory.Adult, 10m, null));
    }

    [Fact]
    public void SeasonPass_Edit_UpdatesFields_AndRoundsPrice()
    {
        var pass = SeasonPass.Create(3, TicketCategory.Adult, 100m, 1);

        pass.Edit(TicketCategory.Youth, 49.999m, TicketStatus.Blocked);

        Assert.Equal(TicketCategory.Youth, pass.Category);
        Assert.Equal(50.00m, pass.Price);
        Assert.Equal(TicketStatus.Blocked, pass.Status);
    }

    [Fact]
    public void SeasonPass_Edit_RejectsNegativePrice()
    {
        var pass = SeasonPass.Create(3, TicketCategory.Adult, 100m, 1);
        Assert.Throws<DomainException>(() => pass.Edit(TicketCategory.Adult, -1m, TicketStatus.Valid));
    }

    [Fact]
    public void MemberCard_Create_RequiresSeason_AndTrimsNames()
    {
        var card = MemberCard.Create(seasonId: 2, MemberCategory.RedAnts, "  Anna ", " Muster ", null);

        Assert.Equal("Anna", card.FirstName);
        Assert.Equal("Muster", card.LastName);
        Assert.Equal("Anna Muster", card.HolderName);
        Assert.Equal(TicketStatus.Valid, card.Status);
    }

    [Fact]
    public void MemberCard_Create_RejectsInvalidSeason()
    {
        Assert.Throws<DomainException>(() => MemberCard.Create(0, MemberCategory.Block4, "A", "B", null));
    }

    [Fact]
    public void MemberCard_Create_BlankNamesBecomeNull()
    {
        var card = MemberCard.Create(2, MemberCategory.RedAnts, "  ", "  ", null, reference: "  ");

        Assert.Null(card.FirstName);
        Assert.Null(card.LastName);
        Assert.Null(card.Reference);
        Assert.Equal(string.Empty, card.HolderName);
    }
}
