namespace RedAnts.Ticketing.Features.Tickets;

public static class GetWebTicketLink
{
    public sealed record Query(Guid Uuid);

    public sealed class Handler(IIssuedTicketReader tickets, ITicketTokens tokens)
    {
        public async Task<string?> HandleAsync(Query query) =>
            await tickets.FindAsync(query.Uuid) is { } issued ? tokens.CreateShort(issued.Uuid) : null;
    }
}
