using RedAnts.Ticketing.Features.Catalog.Admin;

namespace RedAnts.Ticketing.Features.Catalog;

public interface ISeasonStatsReader
{
    Task<SeasonStats> GetAsync(int seasonId, IReadOnlyList<int> eventIds);
}
