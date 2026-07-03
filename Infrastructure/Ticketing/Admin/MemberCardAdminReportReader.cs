using Microsoft.Extensions.DependencyInjection;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Admin;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Admin;

/// <summary>Lists a season's member cards from the <c>MembershipCards</c> table and joins each card's
/// distinct-event visit count from <c>TicketEventVisits</c>. Independent of the ticket repositories.</summary>
public sealed class MemberCardAdminReportReader(IScopeProvider scopeProvider) : IMemberCardAdminReport
{
    public async Task<IReadOnlyList<MemberCardListItem>> GetBySeasonAsync(int seasonId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);

        var cards = await scope.Database.FetchAsync<Sales.MemberCardRecord>(
            "WHERE SeasonId = @0 ORDER BY LastName, FirstName", new object[] { seasonId });

        // Distinct events visited per member card (from the shared visits table; enum stored as int).
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
            (TicketCategory)c.Category,
            c.Price,
            (TicketStatus)c.Status,
            c.CreatedAt,
            visits.GetValueOrDefault(c.Uuid ?? ""))).ToList();
    }

    /// <summary>Projection for the visit-count query (mapped by column name/alias).</summary>
    public sealed class UuidCountRow
    {
        public string Uuid { get; set; } = "";
        public int Cnt { get; set; }
    }
}

/// <summary>Registers the Mitgliederkarten admin report (auto-discovered via <c>.AddComposers()</c>).</summary>
public sealed class MemberCardAdminReportComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IMemberCardAdminReport, MemberCardAdminReportReader>();
}
