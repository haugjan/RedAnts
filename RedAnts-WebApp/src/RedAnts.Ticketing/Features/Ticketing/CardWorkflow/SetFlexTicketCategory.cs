using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.CardWorkflow;

public static class SetFlexTicketCategory
{
    public sealed record Command(Guid Uuid, TicketCategory Category);

    public sealed class Handler(IFlexTicketBundles bundles)
    {
        public Task HandleAsync(Command command) => bundles.SetTicketCategoryAsync(command.Uuid, command.Category);
    }
}
