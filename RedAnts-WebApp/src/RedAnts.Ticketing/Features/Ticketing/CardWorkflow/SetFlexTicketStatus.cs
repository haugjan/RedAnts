using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.CardWorkflow;

public static class SetFlexTicketStatus
{
    public sealed record Command(Guid Uuid, TicketStatus Status);

    public sealed class Handler(IFlexTicketBundles bundles)
    {
        public Task HandleAsync(Command command) => bundles.SetTicketStatusAsync(command.Uuid, command.Status);
    }
}
