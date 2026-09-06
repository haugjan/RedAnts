using NPoco;

namespace RedAnts.Infrastructure.Ticketing.Sales;

internal static class TicketCode
{
    private const string PrefixCountSql =
        "SELECT (SELECT COUNT(1) FROM EventTickets WHERE Uuid LIKE @0)" +
        " + (SELECT COUNT(1) FROM SeasonSingleTickets WHERE Uuid LIKE @0)" +
        " + (SELECT COUNT(1) FROM SeasonPasses WHERE Uuid LIKE @0)" +
        " + (SELECT COUNT(1) FROM MembershipCards WHERE Uuid LIKE @0)";

    public static async Task<Guid> AllocateAsync(IDatabase db, Guid candidate)
    {
        var uuid = candidate;
        for (var attempt = 0; attempt < 25; attempt++)
        {
            var prefix = uuid.ToString("N")[..8] + "%";
            if (await db.ExecuteScalarAsync<int>(PrefixCountSql, prefix) == 0) return uuid;
            uuid = Guid.NewGuid();
        }
        return uuid;
    }
}
