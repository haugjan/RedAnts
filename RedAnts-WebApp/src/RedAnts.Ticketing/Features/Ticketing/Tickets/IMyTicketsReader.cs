using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Tickets;

public sealed record MyTicketSummary(
    TicketType Type,
    Guid Uuid,
    int ScopeId,
    TicketStatus Status,
    DateTimeOffset CreatedAt);

public interface IMyTicketsReader
{
    Task<IReadOnlyList<string>> FindIdentityEmailsAsync(Guid uuid);

    Task<IReadOnlyList<MyTicketSummary>> GetRelatedAsync(IReadOnlyCollection<string> emails);
}
