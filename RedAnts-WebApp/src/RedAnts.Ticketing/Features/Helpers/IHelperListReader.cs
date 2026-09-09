namespace RedAnts.Ticketing.Features.Helpers;

public interface IHelperListReader
{
    Task<IReadOnlyList<HelperRow>> GetBySeasonAsync(int seasonId);
}
