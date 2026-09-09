namespace RedAnts.Ticketing.Features.FlexTickets;

public interface IFlexBundleTicketsReader
{
    Task<IReadOnlyList<FlexTicketRow>> GetByBundleAsync(int bundleId);
    Task<IReadOnlyList<FlexTicketRow>> GetBySeasonAsync(int seasonId);
    Task<IReadOnlyList<FlexTicketRow>> GetByBundlesAsync(IReadOnlyCollection<int> bundleIds);
}
