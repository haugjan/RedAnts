namespace RedAnts.Ticketing.Features.MemberCards;

public interface IMemberCardListReader
{
    Task<IReadOnlyList<MemberCardRow>> GetBySeasonAsync(int seasonId);
}
