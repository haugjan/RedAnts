using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Tickets;

public sealed record MyTicketSummary(
    TicketType Type,
    Guid Uuid,
    int ScopeId,
    TicketStatus Status,
    DateTime CreatedAt);

public interface IMyTicketsReader
{
    Task<IReadOnlyList<MyTicketSummary>> GetByEmailAsync(string email);
}
