using NPoco;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using Umbraco.Cms.Infrastructure.Scoping;

namespace RedAnts.Infrastructure.Ticketing.Sales;

public sealed class EventTicketRepository(IScopeProvider scopeProvider) : IEventTickets
{
    public async Task<IReadOnlyList<EventTicket>> GetByEventAsync(int eventId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<EventTicketRecord>(
            "WHERE EventId = @0 ORDER BY CreatedAt", eventId);
        return rows.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<EventTicket>> GetByOrderAsync(int orderId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var rows = await scope.Database.FetchAsync<EventTicketRecord>(
            "WHERE OrderId = @0 ORDER BY CreatedAt, Id", orderId);
        return rows.Select(Map).ToList();
    }

    public async Task<EventTicket> SaveAsync(EventTicket ticket)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var row = new EventTicketRecord
        {
            Id = ticket.Id,
            Uuid = ticket.Uuid.ToString(),
            EventId = ticket.EventId,
            Category = (int)ticket.Category,
            TierId = ticket.TierId,
            Price = ticket.Price,
            OrderId = ticket.OrderId,
            Status = (int)ticket.Status,
            CreatedAt = ticket.CreatedAt,
            Redeemed = ticket.Redeemed,
            BuyerType = (int?)ticket.Buyer?.Type,
            BuyerFirstName = ticket.Buyer?.FirstName,
            BuyerLastName = ticket.Buyer?.LastName,
            BuyerCompany = ticket.Buyer?.Company,
            CreatedByName = ticket.CreatedByName,
            CreatedByEmail = ticket.CreatedByEmail,
            BundleId = ticket.BundleId
        };
        if (row.Id == 0) await scope.Database.InsertAsync(row);
        else await scope.Database.UpdateAsync(row);
        return Map(row);
    }

    private static EventTicket Map(EventTicketRecord r) =>
        EventTicket.FromPersistence(
            r.Id,
            Guid.TryParse(r.Uuid, out var uuid) ? uuid : Guid.Empty,
            r.EventId,
            (TicketCategory)r.Category,
            r.Price,
            r.OrderId,
            (TicketStatus)r.Status,
            r.CreatedAt,
            r.Redeemed,
            Buyer.FromPersistence(r.BuyerType ?? 0, r.BuyerFirstName, r.BuyerLastName, r.BuyerCompany),
            r.CreatedByName,
            r.CreatedByEmail,
            r.BundleId,
            r.TierId);
}
