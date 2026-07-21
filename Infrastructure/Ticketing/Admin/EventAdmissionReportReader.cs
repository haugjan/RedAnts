using Microsoft.Extensions.DependencyInjection;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Admin;
using RedAnts.Features.Ticketing.Ports;
using RedAnts.Infrastructure.Ticketing.Sales;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Admin;

public sealed class EventAdmissionReportReader(IScopeProvider scopeProvider, IEvents events) : IEventAdmissionReport
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

        var fixedFree = new Dictionary<int, int>();
        foreach (var r in await scope.Database.FetchAsync<EventFreeEntryQuotaRecord>())
        {
            var total = FreeEntryQuotas.FixedTotal(r);
            if (total > 0) fixedFree[r.EventId] = total;
        }

        var passHolders = await Counts(
            "SELECT SeasonId AS EventId, COUNT(*) AS Cnt FROM SeasonPasses WHERE Status = @0 GROUP BY SeasonId",
            (int)TicketStatus.Valid);
        var memberHolders = await Counts(
            "SELECT SeasonId AS EventId, COUNT(*) AS Cnt FROM MembershipCards WHERE Status = @0 GROUP BY SeasonId",
            (int)TicketStatus.Valid);

        var eventToSeason = (await events.GetAllAsync()).ToDictionary(e => e.Id, e => e.SeasonId);

        var ids = new HashSet<int>();
        ids.UnionWith(sold.Keys);
        ids.UnionWith(redeemedEvent.Keys);
        ids.UnionWith(redeemedSingle.Keys);
        ids.UnionWith(passVisits.Keys);
        ids.UnionWith(memberVisits.Keys);
        ids.UnionWith(freeEntries.Keys);
        ids.UnionWith(fixedFree.Keys);
        ids.UnionWith(eventToSeason.Keys);

        var result = new Dictionary<int, EventAdmissionCounts>();
        foreach (var id in ids)
        {
            var seasonId = eventToSeason.GetValueOrDefault(id);
            result[id] = new EventAdmissionCounts(
                sold.GetValueOrDefault(id),
                redeemedEvent.GetValueOrDefault(id),
                redeemedSingle.GetValueOrDefault(id),
                passVisits.GetValueOrDefault(id),
                memberVisits.GetValueOrDefault(id),
                freeEntries.GetValueOrDefault(id) + fixedFree.GetValueOrDefault(id),
                passHolders.GetValueOrDefault(seasonId),
                memberHolders.GetValueOrDefault(seasonId));
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
