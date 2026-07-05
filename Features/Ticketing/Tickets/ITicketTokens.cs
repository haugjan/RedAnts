using RedAnts.Domain.Ticketing.Sales;

namespace RedAnts.Features.Ticketing.Tickets;

public sealed record TicketTokenData(TicketType Type, Guid Uuid, int ScopeId, DateTimeOffset IssuedAt);

public interface ITicketTokens
{
    string Create(TicketType type, Guid uuid, int scopeId);

    bool TryVerify(string token, out TicketTokenData data);
}

public interface IQrCodeRenderer
{
    string RenderSvg(string content, int pixelsPerModule = 6);

    string RenderPngDataUri(string content, int pixelsPerModule = 6);
}

public sealed record IssuedTicket(
    TicketType Type,
    Guid Uuid,
    int ScopeId,
    TicketCategory? Category,
    TicketStatus Status,
    DateTime CreatedAt,
    string? HolderName,
    MemberCategory? MemberCategory = null,
    DateOnly? Birthday = null,
    string? BuyerName = null);

public interface IIssuedTicketReader
{
    Task<IssuedTicket?> FindAsync(Guid uuid);
}
