using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Email;
using RedAnts.Ticketing.Features.MemberCards;
using Xunit;

namespace RedAnts.Ticketing.Tests.Sales;

public class MemberCategoryTests
{
    [Theory]
    [InlineData(MemberCategory.RedAnts, false)]
    [InlineData(MemberCategory.Block4, true)]
    [InlineData(MemberCategory.Company, true)]
    public void Both_block4_categories_count_as_block4(MemberCategory category, bool block4) =>
        Assert.Equal(block4, category.IsBlock4());

    [Theory]
    [InlineData(MemberCategory.RedAnts, TicketingMailKind.MemberCardRedAnts)]
    [InlineData(MemberCategory.Block4, TicketingMailKind.MemberCardBlock4Private)]
    [InlineData(MemberCategory.Company, TicketingMailKind.MemberCardBlock4Company)]
    public void Mail_texts_follow_the_category(MemberCategory category, TicketingMailKind kind) =>
        Assert.Equal(kind, MemberCardMailKinds.For(category));

    [Fact]
    public void Company_is_persisted_as_two() => Assert.Equal(2, (int)MemberCategory.Company);
}
