using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.FlexTickets;

public static class SetFlexTicketCategory
{
    public sealed record Command(Guid Uuid, TicketCategory Category);

    public sealed class Handler(IFlexTicketBundleRepository bundles)
    {
        public Task HandleAsync(Command command) => bundles.SetTicketCategoryAsync(command.Uuid, command.Category);
    }
}
