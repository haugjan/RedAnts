using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Admission;

public interface ITicketRedemptions
{
    Task MarkRedeemedAsync(TicketType ticketType, Guid ticketUuid, int eventId);
}
