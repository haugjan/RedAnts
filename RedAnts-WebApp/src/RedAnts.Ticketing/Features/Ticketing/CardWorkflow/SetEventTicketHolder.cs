using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.CardWorkflow;

public static class SetEventTicketHolder
{
    public sealed record Command(Guid Uuid, CardHolder Holder);

    public sealed class Handler(IEventTickets tickets)
    {
        public Task HandleAsync(Command command) => tickets.SetHolderAsync(command.Uuid, command.Holder);
    }
}
