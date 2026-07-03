using Microsoft.Extensions.DependencyInjection;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Admin;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Admin;

/// <summary>Aggregates per-event admission counts directly from the Sales tables (one grouped query per
/// ticket type), keyed by event id. Kept independent of the ticket repositories so it does not depend
/// on work in progress in other slices.</summary>
public sealed class EventAdmissionReportReader(IScopeProvider scopeProvider) : IEventAdmissionReport
{
    public async Task<IReadOnlyDictionary<int, EventAdmissionCounts>> GetCountsByEventAsync()
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);

        async Task<Dictionary<int, int>> Counts(string sql, params object[] args)
        {
            var rows = await scope.Database.FetchAsync<EventCountRow>(sql, args);
            var map = new Dictionary<int, int>();
            foreach (var r in rows) map[r.EventId] = r.Cnt;
            return map;
        }

        // Sold single (event) tickets: every valid EventTicket issued for the event.
        var sold = await Counts(
            "SELECT EventId, COUNT(*) AS Cnt FROM EventTickets WHERE Status = @0 GROUP BY EventId",
            (int)TicketStatus.Valid);

        // Redeemed EventTickets: admitted at least once (Redeemed flag set on first check-in).
        var redeemedEvent = await Counts(
            "SELECT EventId, COUNT(*) AS Cnt FROM EventTickets WHERE Redeemed = 1 GROUP BY EventId");

        // Season single tickets consumed at this event (bound to it on first check-in).
        var redeemedSingle = await Counts(
            "SELECT RedeemedEventId AS EventId, COUNT(*) AS Cnt FROM SeasonSingleTickets " +
            "WHERE RedeemedEventId IS NOT NULL GROUP BY RedeemedEventId");

        // Multi-event passes/cards that visited this event (distinct cards, from the visits table).
        var passVisits = await Counts(
            "SELECT EventId, COUNT(DISTINCT TicketUuid) AS Cnt FROM TicketEventVisits " +
            "WHERE TicketType = @0 GROUP BY EventId",
            (int)TicketType.SeasonPass);
        var memberVisits = await Counts(
            "SELECT EventId, COUNT(DISTINCT TicketUuid) AS Cnt FROM TicketEventVisits " +
            "WHERE TicketType = @0 GROUP BY EventId",
            (int)TicketType.MemberCard);

        var ids = new HashSet<int>();
        ids.UnionWith(sold.Keys);
        ids.UnionWith(redeemedEvent.Keys);
        ids.UnionWith(redeemedSingle.Keys);
        ids.UnionWith(passVisits.Keys);
        ids.UnionWith(memberVisits.Keys);

        var result = new Dictionary<int, EventAdmissionCounts>();
        foreach (var id in ids)
        {
            result[id] = new EventAdmissionCounts(
                sold.GetValueOrDefault(id),
                redeemedEvent.GetValueOrDefault(id),
                redeemedSingle.GetValueOrDefault(id),
                passVisits.GetValueOrDefault(id),
                memberVisits.GetValueOrDefault(id));
        }
        return result;
    }

    /// <summary>Projection for the grouped count queries (mapped by column name/alias).</summary>
    public sealed class EventCountRow
    {
        public int EventId { get; set; }
        public int Cnt { get; set; }
    }
}

/// <summary>Registers the Anlässe admission report (auto-discovered via <c>.AddComposers()</c>).</summary>
public sealed class EventAdmissionReportComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IEventAdmissionReport, EventAdmissionReportReader>();
}
