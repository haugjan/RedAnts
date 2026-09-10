using RedAnts.Ticketing.Domain.Sales;
using Umbraco.Cms.Core.Services;

namespace RedAnts.Ticketing.Features.Helpers.Infrastructure;

public sealed class HelperMemberRepository(IMemberService memberService) : IHelperRepository
{
    public Task<Helper?> FindByIdAsync(int id)
    {
        var member = memberService.GetById(id);
        return Task.FromResult(member is null || !HelperMembers.IsHelper(member) ? null : HelperMembers.ToHelper(member));
    }

    public Task<Helper?> FindByPasswordAsync(string code)
    {
        var value = (code ?? "").Trim();
        if (value.Length == 0) return Task.FromResult<Helper?>(null);
        var member = memberService.GetByUsername(value);
        return Task.FromResult(member is null || !HelperMembers.IsHelper(member) || !member.IsApproved
            ? null
            : HelperMembers.ToHelper(member));
    }

    public Task<Helper> AddAsync(int seasonId, string firstName, string lastName, string email)
    {
        var code = GenerateUniqueCode();
        var helper = Helper.Create(seasonId, firstName, lastName, email, code);

        var member = memberService.CreateMemberWithIdentity(code, helper.Email.Value, helper.FullName, HelperAliases.MemberType);
        member.IsApproved = true;
        member.SetValue(HelperAliases.Code, code);
        member.SetValue(HelperAliases.FirstName, helper.FirstName);
        member.SetValue(HelperAliases.LastName, helper.LastName);
        member.SetValue(HelperAliases.SeasonId, seasonId);
        member.SetValue(HelperAliases.AllEvents, true);
        member.SetValue(HelperAliases.EventIds, "");
        member.SetValue(HelperAliases.CanRebook, false);
        memberService.Save(member);

        return Task.FromResult(HelperMembers.ToHelper(member));
    }

    public Task SetActiveAsync(int id, bool active)
    {
        var member = memberService.GetById(id);
        if (member is not null && HelperMembers.IsHelper(member))
        {
            member.IsApproved = active;
            memberService.Save(member);
        }
        return Task.CompletedTask;
    }

    public Task SetAssignmentAsync(int id, bool allEvents, IReadOnlyList<int> eventIds, bool canRebook)
    {
        var member = memberService.GetById(id);
        if (member is not null && HelperMembers.IsHelper(member))
        {
            member.SetValue(HelperAliases.AllEvents, allEvents);
            member.SetValue(HelperAliases.EventIds, allEvents ? "" : string.Join(',', eventIds.Where(e => e > 0).Distinct()));
            member.SetValue(HelperAliases.CanRebook, canRebook);
            memberService.Save(member);
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id)
    {
        var member = memberService.GetById(id);
        if (member is not null && HelperMembers.IsHelper(member))
            memberService.Delete(member);
        return Task.CompletedTask;
    }

    private string GenerateUniqueCode()
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var candidate = PasswordGenerator.Generate();
            if (attempt >= 40) candidate += Random.Shared.Next(10, 100);
            if (memberService.GetByUsername(candidate) is null) return candidate;
        }
        return PasswordGenerator.Generate() + Random.Shared.Next(1000, 10000);
    }
}
