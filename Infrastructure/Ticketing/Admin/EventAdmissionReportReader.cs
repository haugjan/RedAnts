using Microsoft.Extensions.DependencyInjection;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Admin;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Admin;

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

        var sold = await Counts(
            "SELECT EventId, COUNT(*) AS Cnt FROM EventTickets WHERE Status = @0 GROUP BY EventId",
            (int)TicketStatus.Valid);

        var redeemedEvent = await Counts(
            "SELECT EventId, COUNT(*) AS Cnt FROM EventTickets WHERE Redeemed = 1 GROUP BY EventId");

        var redeemedSingle = await Counts(
            "SELECT RedeemedEventId AS EventId, COUNT(*) AS Cnt FROM SeasonSingleTickets " +
            "WHERE RedeemedEventId IS NOT NULL GROUP BY RedeemedEventId");

        var passVisits = await Counts(
            "SELECT EventId, COUNT(DISTINCT TicketUuid) AS Cnt FROM TicketEventVisits " +
            "WHERE TicketType = @0 GROUP BY EventId",
            (int)TicketType.SeasonPass);
        var memberVisits = await Counts(
            "SELECT EventId, COUNT(DISTINCT TicketUuid) AS Cnt FROM TicketEventVisits " +
            "WHERE TicketType = @0 GROUP BY EventId",
            (int)TicketType.MemberCard);

        var freeEntries = await Counts(
            "SELECT EventId, COUNT(*) AS Cnt FROM TicketEventVisits " +
            "WHERE TicketType = @0 GROUP BY EventId",
            (int)TicketType.FreeEntry);

        var ids = new HashSet<int>();
        ids.UnionWith(sold.Keys);
        ids.UnionWith(redeemedEvent.Keys);
        ids.UnionWith(redeemedSingle.Keys);
        ids.UnionWith(passVisits.Keys);
        ids.UnionWith(memberVisits.Keys);
        ids.UnionWith(freeEntries.Keys);

        var result = new Dictionary<int, EventAdmissionCounts>();
        foreach (var id in ids)
        {
            result[id] = new EventAdmissionCounts(
                sold.GetValueOrDefault(id),
                redeemedEvent.GetValueOrDefault(id),
                redeemedSingle.GetValueOrDefault(id),
                passVisits.GetValueOrDefault(id),
                memberVisits.GetValueOrDefault(id),
                freeEntries.GetValueOrDefault(id));
        }
        return result;
    }

    public sealed class EventCountRow
    {
        public int EventId { get; set; }
        public int Cnt { get; set; }
    }
}

public sealed class EventAdmissionReportComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IEventAdmissionReport, EventAdmissionReportReader>();
}
