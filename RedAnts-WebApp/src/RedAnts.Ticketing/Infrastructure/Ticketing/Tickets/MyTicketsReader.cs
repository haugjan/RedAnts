using NPoco;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Tickets;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Tickets;

public sealed class MyTicketsReader(IScopeProvider scopeProvider) : IMyTicketsReader
{
    private sealed class TicketRow
    {
        public string Uuid { get; set; } = "";
        public int TicketType { get; set; }
        public int ScopeId { get; set; }
        public int Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public async Task<IReadOnlyList<string>> FindIdentityEmailsAsync(Guid uuid)
    {
        var key = uuid.ToString();
        using var scope = scopeProvider.CreateScope(autoComplete: true);

        const string sql =
            "SELECT Email FROM (" +
            "SELECT o.BillingEmail AS Email FROM EventTickets x LEFT JOIN Orders o ON x.OrderId = o.Id WHERE x.Uuid = @0 " +
            "UNION SELECT x.Email FROM EventTickets x WHERE x.Uuid = @0 " +
            "UNION SELECT o.BillingEmail FROM SeasonSingleTickets x LEFT JOIN Orders o ON x.OrderId = o.Id WHERE x.Uuid = @0 " +
            "UNION SELECT x.BuyerEmail FROM SeasonSingleTickets x WHERE x.Uuid = @0 " +
            "UNION SELECT o.BillingEmail FROM SeasonPasses x LEFT JOIN Orders o ON x.OrderId = o.Id WHERE x.Uuid = @0 " +
            "UNION SELECT x.BuyerEmail FROM SeasonPasses x WHERE x.Uuid = @0 " +
            "UNION SELECT o.BillingEmail FROM MembershipCards x LEFT JOIN Orders o ON x.OrderId = o.Id WHERE x.Uuid = @0 " +
            "UNION SELECT x.Email FROM MembershipCards x WHERE x.Uuid = @0 " +
            ") t WHERE Email IS NOT NULL AND LTRIM(RTRIM(Email)) <> ''";

        var rows = await scope.Database.FetchAsync<string>(sql, key);
        return rows.Select(e => e.Trim().ToLowerInvariant()).Distinct().ToList();
    }

    public async Task<IReadOnlyList<MyTicketSummary>> GetRelatedAsync(IReadOnlyCollection<string> emails)
    {
        var list = emails.Select(e => e.Trim().ToLowerInvariant()).Where(e => e.Length > 0).Distinct().ToList();
        if (list.Count == 0) return [];

        using var scope = scopeProvider.CreateScope(autoComplete: true);

        const string sql =
            "SELECT t.Uuid, t.TicketType, t.ScopeId, t.Status, t.CreatedAt FROM (" +
            "SELECT et.Uuid, 0 AS TicketType, et.EventId AS ScopeId, et.Status, et.CreatedAt " +
            "FROM EventTickets et LEFT JOIN Orders o ON et.OrderId = o.Id " +
            "WHERE o.BillingEmail IN (@0) OR et.Email IN (@0) " +
            "UNION ALL " +
            "SELECT st.Uuid, 1, st.SeasonId, st.Status, st.CreatedAt " +
            "FROM SeasonSingleTickets st LEFT JOIN Orders o ON st.OrderId = o.Id " +
            "WHERE o.BillingEmail IN (@0) OR st.BuyerEmail IN (@0) " +
            "UNION ALL " +
            "SELECT sp.Uuid, 2, sp.SeasonId, sp.Status, sp.CreatedAt " +
            "FROM SeasonPasses sp LEFT JOIN Orders o ON sp.OrderId = o.Id " +
            "WHERE o.BillingEmail IN (@0) OR sp.BuyerEmail IN (@0) " +
            "UNION ALL " +
            "SELECT mc.Uuid, 3, mc.SeasonId, mc.Status, mc.CreatedAt " +
            "FROM MembershipCards mc LEFT JOIN Orders o ON mc.OrderId = o.Id " +
            "WHERE o.BillingEmail IN (@0) OR mc.Email IN (@0) " +
            ") t ORDER BY t.CreatedAt DESC";

        var rows = await scope.Database.FetchAsync<TicketRow>(sql, list);
        return rows
            .Select(r => new MyTicketSummary(
                (TicketType)r.TicketType,
                Guid.Parse(r.Uuid),
                r.ScopeId,
                (TicketStatus)r.Status,
                r.CreatedAt))
            .ToList();
    }
}
