namespace RedAnts.Ticketing.Features.FlexTickets;

public interface IFlexBundleListReader
{
    Task<IReadOnlyList<FlexBundleRow>> GetBySeasonAsync(int seasonId);
}
