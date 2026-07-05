using Microsoft.Extensions.DependencyInjection;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Admin;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Admin;

public sealed class MemberCardAdminReportReader(IScopeProvider scopeProvider) : IMemberCardAdminReport
{
    public async Task<IReadOnlyList<MemberCardListItem>> GetBySeasonAsync(int seasonId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);

        var cards = await scope.Database.FetchAsync<Sales.MemberCardRecord>(
            "WHERE SeasonId = @0 ORDER BY LastName, FirstName", new object[] { seasonId });

        var visitRows = await scope.Database.FetchAsync<UuidCountRow>(
            "SELECT TicketUuid AS Uuid, COUNT(DISTINCT EventId) AS Cnt FROM TicketEventVisits " +
            "WHERE TicketType = @0 AND TicketUuid IS NOT NULL GROUP BY TicketUuid",
            new object[] { (int)TicketType.MemberCard });
        var visits = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var v in visitRows) visits[v.Uuid] = v.Cnt;

        return cards.Select(c => new MemberCardListItem(
            Guid.TryParse(c.Uuid, out var g) ? g : Guid.Empty,
            c.FirstName,
            c.LastName,
            c.Birthday is { } b ? DateOnly.FromDateTime(b) : null,
            (MemberCategory)c.Category,
            (TicketStatus)c.Status,
            c.CreatedAt,
            visits.GetValueOrDefault(c.Uuid ?? ""),
            c.Reference,
            c.CreatedByName)).ToList();
    }

    public sealed class UuidCountRow
    {
        public string Uuid { get; set; } = "";
        public int Cnt { get; set; }
    }
}

public sealed class MemberCardAdminReportComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IMemberCardAdminReport, MemberCardAdminReportReader>();
}
