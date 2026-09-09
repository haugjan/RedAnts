using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Tickets;

public static class GetTicketPrintSettings
{
    public sealed record Query(TicketType Type);

    public sealed class Handler(ITicketPrintSettings settings)
    {
        public Task<TicketPrintLayout> HandleAsync(Query query) => settings.GetAsync(query.Type);
    }
}
