using RedAnts.Domain.Ticketing;

namespace RedAnts.Features.Ticketing.Admin;

public interface IEventStatusEditor
{
    Task SetStatusAsync(int eventId, EventStatus status);
}
