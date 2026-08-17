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
            "SELECT EventId, COUNT(*) AS Cnt FROM TicketEventVisits " +
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
            "SELECT SeasonId AS EventId, SUM(Admissions) AS Cnt FROM MembershipCards WHERE Status = @0 GROUP BY SeasonId",
            (int)TicketStatus.Valid);

        var convRows = await scope.Database.FetchAsync<ConvRow>(
            "SELECT EventId, OriginType AS OriginType, COUNT(*) AS Cnt, " +
            "SUM(CASE WHEN Price > 0 THEN 1 ELSE 0 END) AS PaidCnt, SUM(Price) AS Revenue " +
            "FROM EventTickets WHERE OriginType IS NOT NULL AND Status = @0 GROUP BY EventId, OriginType",
            new object[] { (int)TicketStatus.Valid });
        var convSeason = new Dictionary<int, int>();
        var convMember = new Dictionary<int, int>();
        var convFlex = new Dictionary<int, int>();
        var convPaid = new Dictionary<int, int>();
        var convRevenue = new Dictionary<int, decimal>();
        foreach (var r in convRows)
        {
            if (r.OriginType == (int)TicketType.SeasonPass) convSeason[r.EventId] = r.Cnt;
            else if (r.OriginType == (int)TicketType.MemberCard) convMember[r.EventId] = r.Cnt;
            else if (r.OriginType == (int)TicketType.SeasonSingle) convFlex[r.EventId] = r.Cnt;
            convPaid[r.EventId] = convPaid.GetValueOrDefault(r.EventId) + r.PaidCnt;
            convRevenue[r.EventId] = convRevenue.GetValueOrDefault(r.EventId) + r.Revenue;
        }

        var eventToSeason = (await events.GetAllAsync()).ToDictionary(e => e.Id, e => e.SeasonId);

        var ids = new HashSet<int>();
        ids.UnionWith(sold.Keys);
        ids.UnionWith(redeemedEvent.Keys);
        ids.UnionWith(redeemedSingle.Keys);
        ids.UnionWith(passVisits.Keys);
        ids.UnionWith(memberVisits.Keys);
        ids.UnionWith(freeEntries.Keys);
        ids.UnionWith(fixedFree.Keys);
        ids.UnionWith(convSeason.Keys);
        ids.UnionWith(convMember.Keys);
        ids.UnionWith(convFlex.Keys);
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
                memberHolders.GetValueOrDefault(seasonId),
                convSeason.GetValueOrDefault(id),
                convMember.GetValueOrDefault(id),
                convFlex.GetValueOrDefault(id),
                convPaid.GetValueOrDefault(id),
                convRevenue.GetValueOrDefault(id));
        }
        return result;
    }

    public sealed class EventCountRow
    {
        public int EventId { get; set; }
        public int Cnt { get; set; }
    }

    public sealed class ConvRow
    {
        public int EventId { get; set; }
        public int OriginType { get; set; }
        public int Cnt { get; set; }
        public int PaidCnt { get; set; }
        public decimal Revenue { get; set; }
    }
}

public sealed class EventAdmissionReportComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IEventAdmissionReport, EventAdmissionReportReader>();
}
