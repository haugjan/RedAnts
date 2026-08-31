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

    public async Task SetHolderAsync(Guid uuid, CardHolder holder)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await scope.Database.ExecuteAsync(
            "UPDATE EventTickets SET BuyerType=@0, BuyerFirstName=@1, BuyerLastName=@2, BuyerCompany=@3, Email=@4, " +
            "Salutation=@5, Birthday=@6, Street=@7, AddressLine2=@8, PostalCode=@9, City=@10, Country=@11, Phone=@12 " +
            "WHERE Uuid=@13",
            (object[])new object?[]
            {
                (int)holder.Type, holder.FirstName, holder.LastName, holder.Company, holder.Email,
                holder.Salutation, holder.Birthday is { } b ? b.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                holder.Street, holder.AddressLine2, holder.PostalCode, holder.City, holder.Country, holder.Phone,
                uuid.ToString()
            });
    }

    public async Task<EventTicket> SaveAsync(EventTicket ticket)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var uuid = ticket.Id == 0 ? await TicketCode.AllocateAsync(scope.Database, ticket.Uuid) : ticket.Uuid;
        var row = new EventTicketRecord
        {
            Id = ticket.Id,
            Uuid = uuid.ToString(),
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
            BundleId = ticket.BundleId,
            OriginType = (int?)ticket.OriginType,
            OriginCardUuid = ticket.OriginCardUuid?.ToString()
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
            r.TierId,
            r.OriginType is { } ot ? (TicketType)ot : null,
            Guid.TryParse(r.OriginCardUuid, out var originUuid) ? originUuid : null,
            CardHolder.Create((BuyerType)(r.BuyerType ?? 0), r.Salutation, r.BuyerCompany,
                r.BuyerFirstName, r.BuyerLastName, r.Birthday is { } bd ? DateOnly.FromDateTime(bd) : null,
                r.Email, r.Street, r.AddressLine2, r.PostalCode, r.City, r.Country, r.Phone));
}
