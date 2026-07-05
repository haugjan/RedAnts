namespace RedAnts.Features.Ticketing.Admin;

public sealed record FlexBundleTicket(Guid Uuid, int SeasonId);

public interface IFlexBundleTickets
{
    Task<IReadOnlyList<FlexBundleTicket>> GetByBundleAsync(int bundleId);
}
