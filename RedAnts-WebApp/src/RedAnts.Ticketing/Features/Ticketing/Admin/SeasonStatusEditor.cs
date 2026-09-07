using RedAnts.Domain.Ticketing;

namespace RedAnts.Features.Ticketing.Admin;

public interface ISeasonStatusEditor
{
    Task SetStatusAsync(int seasonId, SeasonStatus status);
}
