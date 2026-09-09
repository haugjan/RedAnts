using RedAnts.Ticketing.Features.MemberCards.Admin;

namespace RedAnts.Ticketing.Features.MemberCards;

public interface IMemberCardAdminReport
{
    Task<IReadOnlyList<MemberCardListItem>> GetBySeasonAsync(int seasonId);
}
