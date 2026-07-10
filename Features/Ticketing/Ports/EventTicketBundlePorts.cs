using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Ports;

public sealed record EventTicketBundleView(
    int Id,
    int EventId,
    TicketCategory Category,
    string Reference,
    DateTime CreatedAt,
    int TicketCount,
    int RedeemedCount,
    string? CreatedByName = null,
    string? CreatedByEmail = null);

public interface IEventTicketBundles
{
    Task<IReadOnlyList<EventTicketBundleView>> GetByEventAsync(int eventId);

    Task<bool> ReferenceExistsAsync(int eventId, string reference);

    Task<EventTicketBundleView> CreateAsync(int eventId, TicketCategory category, string reference, int quantity,
        string? createdByName = null, string? createdByEmail = null);
}
