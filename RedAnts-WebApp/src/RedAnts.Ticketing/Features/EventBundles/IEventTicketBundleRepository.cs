using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Tickets;

namespace RedAnts.Ticketing.Features.EventBundles;

public interface IEventTicketBundleRepository
{
    Task<bool> ReferenceExistsAsync(int eventId, string reference);

    Task<int> CreateAsync(int eventId, TicketCategory category, string reference, int quantity,
        string? createdByName = null, string? createdByEmail = null, int? orderId = null);

    Task<(int Created, int Updated)> ImportUnifiedAsync(int eventId, IReadOnlyList<TicketImportRow> rows,
        string defaultBundle, TicketCategory defaultCategory,
        string? createdByName = null, string? createdByEmail = null);
}
