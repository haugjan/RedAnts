namespace RedAnts.Features.Ticketing.Admin;

public sealed record EventBundleTicket(Guid Uuid, int EventId, string Reference);

public interface IEventBundleTickets
{
    Task<IReadOnlyList<EventBundleTicket>> GetByBundleAsync(int bundleId);
}
