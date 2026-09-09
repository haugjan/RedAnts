using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.EventBundles;

public static class CreateEventTicketBundle
{
    public sealed record Command(int EventId, TicketCategory Category, string Reference, int Quantity,
        string? CreatedByName, string? CreatedByEmail, int? OrderId);

    public sealed class Handler(IEventTicketBundleRepository bundles)
    {
        public async Task<int> HandleAsync(Command command)
        {
            var reference = (command.Reference ?? "").Trim();
            if (reference.Length == 0) throw new DomainException("Bitte ein Bundle angeben.");
            if (command.Quantity < 1) throw new DomainException("Menge muss mindestens 1 sein.");
            if (await bundles.ReferenceExistsAsync(command.EventId, reference))
                throw new DomainException($"Das Bundle „{reference}“ ist für diesen Anlass bereits vergeben.");
            return await bundles.CreateAsync(command.EventId, command.Category, reference, command.Quantity,
                command.CreatedByName, command.CreatedByEmail, command.OrderId);
        }
    }
}
