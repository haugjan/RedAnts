using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.FlexTickets;

public static class SetFlexTicketHolder
{
    public sealed record Command(Guid Uuid, CardHolder Holder);

    public sealed class Handler(IFlexTicketBundles bundles)
    {
        public Task HandleAsync(Command command) => bundles.SetHolderAsync(command.Uuid, command.Holder);
    }
}
