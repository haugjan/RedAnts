using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Ports;

namespace RedAnts.Ticketing.Features.CardWorkflow;

public static class SetFlexTicketHolder
{
    public sealed record Command(Guid Uuid, CardHolder Holder);

    public sealed class Handler(IFlexTicketBundles bundles)
    {
        public Task HandleAsync(Command command) => bundles.SetHolderAsync(command.Uuid, command.Holder);
    }
}
