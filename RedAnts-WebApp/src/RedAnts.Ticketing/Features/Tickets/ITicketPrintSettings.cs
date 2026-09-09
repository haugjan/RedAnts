using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Tickets;

public interface ITicketPrintSettings
{
    Task<TicketPrintLayout> GetAsync(TicketType type);
    Task SaveAsync(TicketType type, TicketPrintLayout layout);
}
