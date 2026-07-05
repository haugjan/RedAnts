namespace RedAnts.Features.Ticketing.Admin;

public sealed record FlexBundleTicket(Guid Uuid, int SeasonId, string Reference);

public interface IFlexBundleTickets
{
    Task<IReadOnlyList<FlexBundleTicket>> GetByBundleAsync(int bundleId);
}
