using RedAnts.Domain.Ticketing;

namespace RedAnts.Features.Ticketing.Ports;

public interface IEventStatusPublisher
{
    Task SetStatusAsync(int eventId, EventStatus status);
}

public interface ISeasonStatusPublisher
{
    Task SetStatusAsync(int seasonId, SeasonStatus status);
}
