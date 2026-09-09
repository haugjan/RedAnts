using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.FlexTickets;

public static class AddFlexTickets
{
    public sealed record Command(int BundleId, TicketCategory Category, int Quantity, string? CreatedByName, string? CreatedByEmail, int? OrderId);

    public sealed class Handler(IFlexTicketBundleRepository bundles)
    {
        public Task<int> HandleAsync(Command command)
        {
            if (command.Quantity < 1) throw new DomainException("Menge muss mindestens 1 sein.");
            return bundles.AddTicketsAsync(command.BundleId, command.Category, command.Quantity,
                command.CreatedByName, command.CreatedByEmail, command.OrderId);
        }
    }
}
