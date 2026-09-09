using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Tickets;

namespace RedAnts.Ticketing.Features.EventBundles;

public sealed record EventTicketBundleView(
    int Id,
    int EventId,
    TicketCategory Category,
    string Reference,
    DateTimeOffset CreatedAt,
    int TicketCount,
    int RedeemedCount,
    string? CreatedByName = null,
    string? CreatedByEmail = null);

public interface IEventTicketBundles
{
    Task<IReadOnlyList<EventTicketBundleView>> GetByEventAsync(int eventId);

    Task<bool> ReferenceExistsAsync(int eventId, string reference);

    Task<EventTicketBundleView> CreateAsync(int eventId, TicketCategory category, string reference, int quantity,
        string? createdByName = null, string? createdByEmail = null, int? orderId = null);

    Task<(int Created, int Updated)> ImportUnifiedAsync(int eventId, IReadOnlyList<TicketImportRow> rows,
        string defaultBundle, TicketCategory defaultCategory,
        string? createdByName = null, string? createdByEmail = null);
}
