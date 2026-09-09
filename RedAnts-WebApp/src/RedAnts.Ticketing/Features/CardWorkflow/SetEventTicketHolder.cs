using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Ports;

namespace RedAnts.Ticketing.Features.CardWorkflow;

public static class SetEventTicketHolder
{
    public sealed record Command(Guid Uuid, CardHolder Holder);

    public sealed class Handler(IEventTickets tickets)
    {
        public Task HandleAsync(Command command) => tickets.SetHolderAsync(command.Uuid, command.Holder);
    }
}
