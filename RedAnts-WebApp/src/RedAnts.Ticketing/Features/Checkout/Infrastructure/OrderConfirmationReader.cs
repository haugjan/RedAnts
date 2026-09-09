using NPoco;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Ticketing.Features.Checkout.Infrastructure;

public sealed class OrderConfirmationReader(IScopeProvider scopeProvider) : IOrderConfirmationReader
{
    public async Task<IReadOnlyList<ConfirmedEventTicket>> GetEventTicketsAsync(int orderId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<ConfirmedTicketRow>(
            "SELECT Uuid, EventId AS ScopeId, TierId FROM EventTickets WHERE OrderId = @0 ORDER BY CreatedAt, Id", orderId);
        return rows.Select(r => new ConfirmedEventTicket(Guid.Parse(r.Uuid), r.ScopeId, r.TierId)).ToList();
    }

    public async Task<IReadOnlyList<ConfirmedSeasonPass>> GetSeasonPassesAsync(int orderId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<ConfirmedTicketRow>(
            "SELECT Uuid, SeasonId AS ScopeId, TierId FROM SeasonPasses WHERE OrderId = @0 ORDER BY CreatedAt, Id", orderId);
        return rows.Select(r => new ConfirmedSeasonPass(Guid.Parse(r.Uuid), r.ScopeId, r.TierId)).ToList();
    }
}

public sealed class ConfirmedTicketRow
{
    public string Uuid { get; set; } = "";
    public int ScopeId { get; set; }
    public int? TierId { get; set; }
}
