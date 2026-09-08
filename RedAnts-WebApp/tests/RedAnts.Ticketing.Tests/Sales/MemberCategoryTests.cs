using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Email;
using Xunit;

namespace RedAnts.Ticketing.Tests.Sales;

public class MemberCategoryTests
{
    [Theory]
    [InlineData(MemberCategory.RedAnts, "Red Ants", false)]
    [InlineData(MemberCategory.Block4, "Block 4 einzel", true)]
    [InlineData(MemberCategory.Company, "Block 4 Firma", true)]
    public void Each_category_has_a_label_and_a_block4_flag(MemberCategory category, string label, bool block4)
    {
        Assert.Equal(label, category.DisplayName());
        Assert.Equal(block4, category.IsBlock4());
    }

    [Theory]
    [InlineData(MemberCategory.RedAnts, TicketingMailKind.MemberCardRedAnts)]
    [InlineData(MemberCategory.Block4, TicketingMailKind.MemberCardBlock4Private)]
    [InlineData(MemberCategory.Company, TicketingMailKind.MemberCardBlock4Company)]
    public void Mail_texts_follow_the_category(MemberCategory category, TicketingMailKind kind) =>
        Assert.Equal(kind, MemberCardMailKinds.For(category));

    [Fact]
    public void Company_is_persisted_as_two() => Assert.Equal(2, (int)MemberCategory.Company);
}
