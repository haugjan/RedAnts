using RedAnts.Ticketing.Features.Tickets;

namespace RedAnts.Ticketing.Features.Admission;

public sealed record ScannedCode(TicketTokenData? Token, string? ShortCode);

public static class ResolveScannedCode
{
    public sealed record Query(string Scanned);

    public sealed class Handler(ITicketTokens tokens)
    {
        public Task<ScannedCode> HandleAsync(Query query) => Task.FromResult(
            tokens.TryVerify(query.Scanned, out var data) ? new ScannedCode(data, null)
            : tokens.TryVerifyShort(query.Scanned, out var shortCode) ? new ScannedCode(null, shortCode)
            : new ScannedCode(null, null));
    }
}
