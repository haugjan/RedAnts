using Umbraco.Cms.Core.Services;

namespace RedAnts.Ticketing.Features.Helpers.Infrastructure;

public sealed class HelperListReader(IMemberService memberService) : IHelperListReader
{
    public Task<IReadOnlyList<HelperRow>> GetBySeasonAsync(int seasonId)
    {
        var helpers = memberService.GetMembersByMemberType(HelperAliases.MemberType)
            .Select(HelperMembers.ToHelper)
            .Where(h => h.SeasonId == seasonId)
            .OrderByDescending(h => h.Active).ThenBy(h => h.LastName).ThenBy(h => h.FirstName).ThenBy(h => h.Id)
            .Select(HelperMembers.ToRow)
            .ToList();
        return Task.FromResult<IReadOnlyList<HelperRow>>(helpers);
    }
}
