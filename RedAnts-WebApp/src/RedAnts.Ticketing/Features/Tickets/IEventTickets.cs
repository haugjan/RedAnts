using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Tickets;

public interface IEventTickets
{
    Task<IReadOnlyList<EventTicket>> GetByEventAsync(int eventId);
    Task<IReadOnlyList<EventTicket>> GetByOrderAsync(int orderId);
    Task<EventTicket> SaveAsync(EventTicket ticket);
    Task SetHolderAsync(Guid uuid, CardHolder holder);
}
