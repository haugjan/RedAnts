using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Ports;

namespace RedAnts.Ticketing.Features.CardWorkflow;

public static class SetFlexTicketCategory
{
    public sealed record Command(Guid Uuid, TicketCategory Category);

    public sealed class Handler(IFlexTicketBundles bundles)
    {
        public Task HandleAsync(Command command) => bundles.SetTicketCategoryAsync(command.Uuid, command.Category);
    }
}
