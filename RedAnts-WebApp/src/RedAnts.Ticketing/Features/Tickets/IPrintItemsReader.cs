using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Tickets;

public interface IPrintItemsReader
{
    Task<IReadOnlyList<TicketPrintItem>> ResolveAsync(TicketType type, int? bundleId, int? seasonId, string? reference, Guid? uuid);
}
