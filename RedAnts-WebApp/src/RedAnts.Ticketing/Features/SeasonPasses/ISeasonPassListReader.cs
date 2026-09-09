namespace RedAnts.Ticketing.Features.SeasonPasses;

public interface ISeasonPassListReader
{
    Task<IReadOnlyList<SeasonPassRow>> GetBySeasonAsync(int seasonId);
}
