using RedAnts.Domain.Ticketing;

namespace RedAnts.Features.Ticketing.Admin;

public interface ISeasonStatusEditor
{
    Task SetStatusAsync(int seasonId, SeasonStatus status);
    Task SetNameAsync(int seasonId, string name);
    Task SetPeriodAsync(int seasonId, DateOnly startDate, DateOnly endDate);
}
