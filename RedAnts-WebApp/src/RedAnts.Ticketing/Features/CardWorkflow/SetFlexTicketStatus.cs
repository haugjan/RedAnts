using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Ports;

namespace RedAnts.Ticketing.Features.CardWorkflow;

public static class SetFlexTicketStatus
{
    public sealed record Command(Guid Uuid, TicketStatus Status);

    public sealed class Handler(IFlexTicketBundles bundles)
    {
        public Task HandleAsync(Command command) => bundles.SetTicketStatusAsync(command.Uuid, command.Status);
    }
}
