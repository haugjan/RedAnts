using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using RedAnts.Infrastructure.Ticketing.Content;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace RedAnts.Infrastructure.Ticketing.Sales;

public sealed class HelperMemberRepository(IMemberService memberService) : IHelpers
{
    public Task<IReadOnlyList<Helper>> GetBySeasonAsync(int seasonId)
    {
        var helpers = memberService.GetMembersByMemberType(HelperAliases.MemberType)
            .Select(Map)
            .Where(h => h.SeasonId == seasonId)
            .OrderByDescending(h => h.Active).ThenBy(h => h.LastName).ThenBy(h => h.FirstName).ThenBy(h => h.Id)
            .ToList();
        return Task.FromResult<IReadOnlyList<Helper>>(helpers);
    }

    public Task<Helper?> FindByIdAsync(int id)
    {
        var member = memberService.GetById(id);
        return Task.FromResult(member is null || member.ContentType.Alias != HelperAliases.MemberType ? null : Map(member));
    }

    public Task<Helper?> FindByPasswordAsync(string code)
    {
        var value = (code ?? "").Trim();
        if (value.Length == 0) return Task.FromResult<Helper?>(null);
        var member = memberService.GetByUsername(value);
        return Task.FromResult(member is null || member.ContentType.Alias != HelperAliases.MemberType || !member.IsApproved
            ? null
            : Map(member));
    }

    public Task<Helper> AddAsync(int seasonId, string firstName, string lastName, string email)
    {
        var code = GenerateUniqueCode();
        var helper = Helper.Create(seasonId, firstName, lastName, email, code);

        var member = memberService.CreateMemberWithIdentity(code, helper.Email, helper.FullName, HelperAliases.MemberType);
        member.IsApproved = true;
        member.SetValue(HelperAliases.Code, code);
        member.SetValue(HelperAliases.FirstName, helper.FirstName);
        member.SetValue(HelperAliases.LastName, helper.LastName);
        member.SetValue(HelperAliases.SeasonId, seasonId);
        member.SetValue(HelperAliases.AllEvents, true);
        member.SetValue(HelperAliases.EventIds, "");
        memberService.Save(member);

        return Task.FromResult(Map(member));
    }

    public Task SetActiveAsync(int id, bool active)
    {
        var member = memberService.GetById(id);
        if (member is not null && member.ContentType.Alias == HelperAliases.MemberType)
        {
            member.IsApproved = active;
            memberService.Save(member);
        }
        return Task.CompletedTask;
    }

    public Task SetAssignmentAsync(int id, bool allEvents, IReadOnlyList<int> eventIds)
    {
        var member = memberService.GetById(id);
        if (member is not null && member.ContentType.Alias == HelperAliases.MemberType)
        {
            member.SetValue(HelperAliases.AllEvents, allEvents);
            member.SetValue(HelperAliases.EventIds, allEvents ? "" : string.Join(',', eventIds.Where(e => e > 0).Distinct()));
            memberService.Save(member);
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id)
    {
        var member = memberService.GetById(id);
        if (member is not null && member.ContentType.Alias == HelperAliases.MemberType)
            memberService.Delete(member);
        return Task.CompletedTask;
    }

    private string GenerateUniqueCode()
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var candidate = HelperPassword.Generate();
            if (attempt >= 40) candidate += Random.Shared.Next(10, 100);
            if (memberService.GetByUsername(candidate) is null) return candidate;
        }
        return HelperPassword.Generate() + Random.Shared.Next(1000, 10000);
    }

    private static Helper Map(IMember m) =>
        Helper.FromPersistence(
            m.Id,
            m.GetValue<int?>(HelperAliases.SeasonId) ?? 0,
            m.GetValue<string>(HelperAliases.FirstName) ?? "",
            m.GetValue<string>(HelperAliases.LastName) ?? "",
            m.Email ?? "",
            string.IsNullOrWhiteSpace(m.GetValue<string>(HelperAliases.Code)) ? m.Username : m.GetValue<string>(HelperAliases.Code)!,
            m.GetValue<bool>(HelperAliases.AllEvents),
            ParseIds(m.GetValue<string>(HelperAliases.EventIds)),
            m.IsApproved,
            m.CreateDate);

    private static IReadOnlyList<int> ParseIds(string? csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? []
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();
}
