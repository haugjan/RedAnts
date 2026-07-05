using NPoco;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Tickets;
using RedAnts.Infrastructure.Ticketing.Sales;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Tickets;

/// <summary>Finds an issued ticket by its Uuid by probing the four ticket tables in turn (each has a
/// unique index on Uuid). Reuses S4's NPoco record types read-only; it does not go through the
/// per-type sales repositories, so it stays inside this slice.</summary>
public sealed class IssuedTicketReader(IScopeProvider scopeProvider) : IIssuedTicketReader
{
    public async Task<IssuedTicket?> FindAsync(Guid uuid)
    {
        var key = uuid.ToString();
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;

        var eventTicket = await db.FirstOrDefaultAsync<EventTicketRecord>("WHERE Uuid = @0", key);
        if (eventTicket is not null)
            return new IssuedTicket(TicketType.EventTicket, uuid, eventTicket.EventId,
                (TicketCategory)eventTicket.Category, (TicketStatus)eventTicket.Status, eventTicket.CreatedAt, null);

        var single = await db.FirstOrDefaultAsync<SeasonSingleTicketRecord>("WHERE Uuid = @0", key);
        if (single is not null)
            return new IssuedTicket(TicketType.SeasonSingle, uuid, single.SeasonId,
                (TicketCategory)single.Category, (TicketStatus)single.Status, single.CreatedAt, null);

        var pass = await db.FirstOrDefaultAsync<SeasonPassRecord>("WHERE Uuid = @0", key);
        if (pass is not null)
            return new IssuedTicket(TicketType.SeasonPass, uuid, pass.SeasonId,
                (TicketCategory)pass.Category, (TicketStatus)pass.Status, pass.CreatedAt, null);

        var card = await db.FirstOrDefaultAsync<MemberCardRecord>("WHERE Uuid = @0", key);
        if (card is not null)
        {
            var holder = string.Join(' ', new[] { card.FirstName, card.LastName }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
            return new IssuedTicket(TicketType.MemberCard, uuid, card.SeasonId,
                null, (TicketStatus)card.Status, card.CreatedAt,
                string.IsNullOrWhiteSpace(holder) ? null : holder,
                (MemberCategory)card.Category);
        }

        return null;
    }
}
