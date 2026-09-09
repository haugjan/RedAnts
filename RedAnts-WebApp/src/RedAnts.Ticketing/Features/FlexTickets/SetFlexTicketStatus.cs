using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.FlexTickets;

public static class SetFlexTicketStatus
{
    public sealed record Command(Guid Uuid, TicketStatus Status);

    public sealed class Handler(IFlexTicketBundles bundles)
    {
        public Task HandleAsync(Command command) => bundles.SetTicketStatusAsync(command.Uuid, command.Status);
    }
}
