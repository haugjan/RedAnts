using Microsoft.Extensions.DependencyInjection;
using NPoco;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Admin;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Admin;

public sealed class OrderTicketDeactivator(IScopeProvider scopeProvider) : IOrderTickets
{
    private static readonly string[] Tables =
        ["EventTickets", "SeasonSingleTickets", "SeasonPasses", "MembershipCards"];

    public async Task<int> DeactivateByOrderAsync(int orderId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        var total = 0;
        foreach (var table in Tables)
            total += await db.ExecuteAsync(
                $"UPDATE {table} SET Status = @0 WHERE OrderId = @1 AND Status = @2",
                (int)TicketStatus.Cancelled, orderId, (int)TicketStatus.Valid);
        return total;
    }
}

public sealed class OrderTicketsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IOrderTickets, OrderTicketDeactivator>();
}
