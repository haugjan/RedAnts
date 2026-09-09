using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Tickets;

public static class CreateEventTicket
{
    public sealed record Command(
        int EventId, TicketCategory Category, decimal Price, int OrderId, Buyer Buyer,
        string? CreatedByName, string? CreatedByEmail, CardHolder Holder);

    public sealed class Handler(IEventTicketRepository tickets)
    {
        public async Task<Guid> HandleAsync(Command command)
        {
            if (command.Price < 0) throw new DomainException("Der Abgabepreis darf nicht negativ sein.");
            var saved = await tickets.SaveAsync(EventTicket.Create(command.EventId, command.Category, command.Price, command.OrderId,
                command.Buyer, command.CreatedByName, command.CreatedByEmail));
            await tickets.SetHolderAsync(saved.Uuid, command.Holder);
            return saved.Uuid;
        }
    }
}
