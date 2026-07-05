using RedAnts.Domain.Ticketing;
using RedAnts.Domain.Ticketing.Sales;
using Xunit;

namespace RedAnts.Domain.Tests.Sales;

public class FlexTicketBundleTests
{
    [Fact]
    public void Create_TrimsReference()
    {
        var bundle = FlexTicketBundle.Create(seasonId: 3, TicketCategory.Adult, "  Sponsor A  ");
        Assert.Equal("Sponsor A", bundle.Reference);
    }

    [Fact]
    public void Create_RejectsInvalidSeason()
    {
        Assert.Throws<DomainException>(() => FlexTicketBundle.Create(0, TicketCategory.Adult, "Ref"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsBlankReference(string reference)
    {
        Assert.Throws<DomainException>(() => FlexTicketBundle.Create(3, TicketCategory.Adult, reference));
    }

    [Fact]
    public void Create_RejectsReferenceOverMaxLength()
    {
        var tooLong = new string('x', FlexTicketBundle.ReferenceMaxLength + 1);
        Assert.Throws<DomainException>(() => FlexTicketBundle.Create(3, TicketCategory.Adult, tooLong));
    }

    [Fact]
    public void Create_AcceptsReferenceAtMaxLength()
    {
        var atLimit = new string('x', FlexTicketBundle.ReferenceMaxLength);
        var bundle = FlexTicketBundle.Create(3, TicketCategory.Adult, atLimit);
        Assert.Equal(atLimit, bundle.Reference);
    }
}
