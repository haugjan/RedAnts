using RedAnts.Ticketing.Domain.Sales;
using Umbraco.Cms.Core.Models;

namespace RedAnts.Ticketing.Features.Helpers.Infrastructure;

internal static class HelperMembers
{
    public static bool IsHelper(IMember member) => member.ContentType.Alias == HelperAliases.MemberType;

    public static Helper ToHelper(IMember m) =>
        Helper.FromPersistence(
            m.Id,
            m.GetValue<int?>(HelperAliases.SeasonId) ?? 0,
            m.GetValue<string>(HelperAliases.FirstName) ?? "",
            m.GetValue<string>(HelperAliases.LastName) ?? "",
            m.Email ?? "",
            string.IsNullOrWhiteSpace(m.GetValue<string>(HelperAliases.Code)) ? m.Username : m.GetValue<string>(HelperAliases.Code)!,
            m.GetValue<bool>(HelperAliases.AllEvents),
            ParseIds(m.GetValue<string>(HelperAliases.EventIds)),
            m.GetValue<bool>(HelperAliases.CanRebook),
            m.IsApproved,
            m.CreateDate);

    public static HelperRow ToRow(Helper h) =>
        new(h.Id, h.SeasonId, h.FirstName, h.LastName, h.Email, h.Code, h.AllEvents, h.EventIds, h.CanRebook, h.Active, h.CreatedAt);

    private static IReadOnlyList<int> ParseIds(string? csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? []
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();
}
