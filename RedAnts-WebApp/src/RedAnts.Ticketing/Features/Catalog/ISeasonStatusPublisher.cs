using RedAnts.Ticketing.Domain;

namespace RedAnts.Ticketing.Features.Catalog;

public interface ISeasonStatusPublisher
{
    Task SetStatusAsync(int seasonId, SeasonStatus status);
}
