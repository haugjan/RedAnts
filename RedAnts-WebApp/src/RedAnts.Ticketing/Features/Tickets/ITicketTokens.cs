using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Tickets;

public sealed record TicketTokenData(TicketType Type, Guid Uuid, int ScopeId, DateTimeOffset IssuedAt);

public interface ITicketTokens
{
    string Create(TicketType type, Guid uuid, int scopeId);

    bool TryVerify(string token, out TicketTokenData data);

    string CreateShort(Guid uuid);

    bool TryVerifyShort(string token, out string code);
}
