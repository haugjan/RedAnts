namespace RedAnts.Ticketing.Features.Tickets;

public interface IEventTicketListReader
{
    Task<IReadOnlyList<EventTicketRow>> GetByEventAsync(int eventId);
    Task<IReadOnlyList<EventTicketBundleRow>> GetBundlesByEventAsync(int eventId);
}
