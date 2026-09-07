using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.CardWorkflow;

public static class SetFlexTicketHolder
{
    public sealed record Command(Guid Uuid, CardHolder Holder);

    public sealed class Handler(IFlexTicketBundles bundles)
    {
        public Task HandleAsync(Command command) => bundles.SetHolderAsync(command.Uuid, command.Holder);
    }
}
