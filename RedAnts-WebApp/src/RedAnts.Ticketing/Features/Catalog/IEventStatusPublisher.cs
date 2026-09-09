using RedAnts.Ticketing.Domain;

namespace RedAnts.Ticketing.Features.Catalog;

public interface IEventStatusPublisher
{
    Task SetStatusAsync(int eventId, EventStatus status);
}
