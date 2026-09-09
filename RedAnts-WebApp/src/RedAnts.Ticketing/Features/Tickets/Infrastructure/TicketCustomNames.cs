using NPoco;
using RedAnts.Ticketing.Domain.Sales;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Features.Tickets.Infrastructure;

public sealed class TicketCustomNames(IScopeProvider scopeProvider) : ITicketCustomNames
{
    public async Task SetAsync(TicketType type, Guid uuid, string? customName)
    {
        var table = type switch
        {
            TicketType.EventTicket => "EventTickets",
            TicketType.SeasonSingle => "SeasonSingleTickets",
            TicketType.SeasonPass => "SeasonPasses",
            TicketType.MemberCard => "MembershipCards",
            _ => null
        };
        if (table is null) return;

        var trimmed = customName?.Trim();
        var value = string.IsNullOrEmpty(trimmed) ? null
            : trimmed.Length > 120 ? trimmed[..120] : trimmed;

        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await scope.Database.ExecuteAsync($"UPDATE {table} SET CustomName = @0 WHERE Uuid = @1", value, uuid.ToString());
    }
}
