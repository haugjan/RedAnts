using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Tickets;

public sealed record IssuedTicket(
    TicketType Type,
    Guid Uuid,
    int ScopeId,
    TicketCategory? Category,
    TicketStatus Status,
    DateTimeOffset CreatedAt,
    string? HolderName,
    MemberCategory? MemberCategory = null,
    DateOnly? Birthday = null,
    string? BuyerName = null,
    string? CategoryName = null,
    int Admissions = 1,
    string? CustomName = null);

public interface IIssuedTicketReader
{
    Task<IssuedTicket?> FindAsync(Guid uuid);

    Task<IssuedTicket?> FindByCodeAsync(string codePrefix);
}
