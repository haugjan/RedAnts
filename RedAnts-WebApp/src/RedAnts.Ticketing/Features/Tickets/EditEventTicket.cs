using RedAnts.Ticketing.Domain.Sales;

namespace RedAnts.Ticketing.Features.Tickets;

public static class EditEventTicket
{
    public sealed record Command(Guid Uuid, int EventId, TicketCategory Category, decimal Price, TicketStatus Status, bool Redeemed, Buyer? Buyer);

    public sealed class Handler(IEventTicketRepository tickets)
    {
        public async Task HandleAsync(Command command)
        {
            if (command.Price < 0) throw new DomainException("Der Abgabepreis darf nicht leer oder negativ sein.");
            var ticket = await tickets.GetByUuidAsync(command.Uuid)
                ?? throw new DomainException("Ticket wurde nicht gefunden.");
            var redeemed = command.Status == TicketStatus.Valid && command.Redeemed;
            await tickets.SaveAsync(EventTicket.FromPersistence(ticket.Id, ticket.Uuid, ticket.EventId, command.Category,
                decimal.Round(command.Price, 2), ticket.OrderId, command.Status, ticket.CreatedAt, redeemed,
                command.Buyer ?? ticket.Buyer, ticket.CreatedByName, ticket.CreatedByEmail, ticket.BundleId,
                ticket.TierId, ticket.OriginType, ticket.OriginCardUuid, ticket.Holder));
        }
    }
}
