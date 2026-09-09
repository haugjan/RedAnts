using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Tickets;

public interface IEventTicketRepository
{
    Task<EventTicket?> GetByUuidAsync(Guid uuid);
    Task<EventTicket> SaveAsync(EventTicket ticket);
    Task SetHolderAsync(Guid uuid, CardHolder holder);
}
