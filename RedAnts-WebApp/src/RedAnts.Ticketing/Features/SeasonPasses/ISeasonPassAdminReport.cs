using RedAnts.Ticketing.Features.SeasonPasses.Admin;

namespace RedAnts.Ticketing.Features.SeasonPasses;

public interface ISeasonPassAdminReport
{
    Task<IReadOnlyList<SeasonPassListItem>> GetBySeasonAsync(int seasonId);
}
