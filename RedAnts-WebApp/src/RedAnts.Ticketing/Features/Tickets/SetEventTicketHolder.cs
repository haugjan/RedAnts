using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Tickets;

public static class SetEventTicketHolder
{
    public sealed record Command(Guid Uuid, CardHolder Holder);

    public sealed class Handler(IEventTickets tickets)
    {
        public Task HandleAsync(Command command) => tickets.SetHolderAsync(command.Uuid, command.Holder);
    }
}
