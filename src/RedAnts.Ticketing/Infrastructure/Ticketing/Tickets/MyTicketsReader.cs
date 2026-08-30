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

    public async Task<IReadOnlyList<MyTicketSummary>> GetByEmailAsync(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;

        const string sql =
            "SELECT t.Uuid, t.TicketType, t.ScopeId, t.Status, t.CreatedAt FROM (" +
            "SELECT et.Uuid, 0 AS TicketType, et.EventId AS ScopeId, et.Status, et.CreatedAt " +
            "FROM EventTickets et INNER JOIN Orders o ON et.OrderId = o.Id " +
            "WHERE LOWER(o.BillingEmail) = @0 " +
            "UNION ALL " +
            "SELECT st.Uuid, 1 AS TicketType, st.SeasonId AS ScopeId, st.Status, st.CreatedAt " +
            "FROM SeasonSingleTickets st INNER JOIN Orders o ON st.OrderId = o.Id " +
            "WHERE LOWER(o.BillingEmail) = @0 " +
            "UNION ALL " +
            "SELECT sp.Uuid, 2 AS TicketType, sp.SeasonId AS ScopeId, sp.Status, sp.CreatedAt " +
            "FROM SeasonPasses sp INNER JOIN Orders o ON sp.OrderId = o.Id " +
            "WHERE LOWER(o.BillingEmail) = @0 " +
            "UNION ALL " +
            "SELECT mc.Uuid, 3 AS TicketType, mc.SeasonId AS ScopeId, mc.Status, mc.CreatedAt " +
            "FROM MembershipCards mc INNER JOIN Orders o ON mc.OrderId = o.Id " +
            "WHERE LOWER(o.BillingEmail) = @0 " +
            ") t ORDER BY t.CreatedAt DESC";

        var rows = await db.FetchAsync<TicketRow>(sql, normalized);
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
