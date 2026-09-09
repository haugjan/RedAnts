using RedAnts.Ticketing.Domain;

namespace RedAnts.Ticketing.Features.Ports;

public interface IEventStatusPublisher
{
    Task SetStatusAsync(int eventId, EventStatus status);
}

public interface ISeasonStatusPublisher
{
    Task SetStatusAsync(int seasonId, SeasonStatus status);
}
