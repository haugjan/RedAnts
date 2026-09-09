using RedAnts.Ticketing.Domain.Sales;
using RedAnts.Ticketing.Features.Ports;

namespace RedAnts.Ticketing.Features.CardWorkflow;

public static class AddFlexTickets
{
    public sealed record Command(int BundleId, TicketCategory Category, int Quantity, string? CreatedByName, string? CreatedByEmail, int? OrderId);

    public sealed class Handler(IFlexTicketBundles bundles)
    {
        public Task<FlexTicketBundleView> HandleAsync(Command command)
        {
            if (command.Quantity < 1) throw new DomainException("Menge muss mindestens 1 sein.");
            return bundles.AddTicketsAsync(command.BundleId, command.Category, command.Quantity,
                command.CreatedByName, command.CreatedByEmail, command.OrderId);
        }
    }
}
