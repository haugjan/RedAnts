using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Tickets;

public interface ITicketCustomNames
{
    Task SetAsync(TicketType type, Guid uuid, string? customName);
}
