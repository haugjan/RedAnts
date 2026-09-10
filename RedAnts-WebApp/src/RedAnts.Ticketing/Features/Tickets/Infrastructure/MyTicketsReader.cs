using NPoco;
using RedAnts.Ticketing.Domain.Sales;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Features.Tickets.Infrastructure;

public sealed class MyTicketsReader(IScopeProvider scopeProvider) : IMyTicketsReader
{
    private sealed class TicketRow
    {
        public string Uuid { get; set; } = "";
        public int TicketType { get; set; }
        public int ScopeId { get; set; }
        public int Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public async Task<IReadOnlyList<string>> FindIdentityEmailsAsync(Guid uuid)
    {
        var key = uuid.ToString();
        using var scope = scopeProvider.CreateScope(autoComplete: true);

        const string sql =
            "SELECT Email FROM (" +
            "SELECT Email FROM EventTickets WHERE Uuid = @0 " +
            "UNION SELECT BuyerEmail FROM SeasonSingleTickets WHERE Uuid = @0 " +
            "UNION SELECT BuyerEmail FROM SeasonPasses WHERE Uuid = @0 " +
            "UNION SELECT Email FROM MembershipCards WHERE Uuid = @0 " +
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
            "SELECT Uuid, 0 AS TicketType, EventId AS ScopeId, Status, CreatedAt FROM EventTickets WHERE Email IN (@0) " +
            "UNION ALL SELECT Uuid, 1, SeasonId, Status, CreatedAt FROM SeasonSingleTickets WHERE BuyerEmail IN (@0) " +
            "UNION ALL SELECT Uuid, 2, SeasonId, Status, CreatedAt FROM SeasonPasses WHERE BuyerEmail IN (@0) " +
            "UNION ALL SELECT Uuid, 3, SeasonId, Status, CreatedAt FROM MembershipCards WHERE Email IN (@0) " +
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
