using NPoco;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Scanning;

public sealed class TicketRedemptions(IScopeProvider scopeProvider) : ITicketRedemptions
{
    public async Task MarkRedeemedAsync(TicketType ticketType, Guid ticketUuid, int eventId)
    {
        if (ticketType is not (TicketType.SeasonSingle or TicketType.EventTicket)) return;
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var key = ticketUuid.ToString();
        if (ticketType == TicketType.SeasonSingle)
            await scope.Database.ExecuteAsync(
                "UPDATE SeasonSingleTickets SET RedeemedEventId = @0, Redeemed = 1 WHERE Uuid = @1 AND RedeemedEventId IS NULL",
                eventId, key);
        else
            await scope.Database.ExecuteAsync("UPDATE EventTickets SET Redeemed = 1 WHERE Uuid = @0", key);
    }
}
