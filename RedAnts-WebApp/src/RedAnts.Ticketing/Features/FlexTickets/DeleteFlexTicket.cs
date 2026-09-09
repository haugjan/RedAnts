using RedAnts.Ticketing.Features.Tickets;

namespace RedAnts.Ticketing.Features.FlexTickets;

public static class DeleteFlexTicket
{
    public sealed record Command(Guid Uuid);

    public sealed class Handler(IAdminTicketDeletion deletion)
    {
        public Task HandleAsync(Command command) => deletion.DeleteFlexTicketAsync(command.Uuid);
    }
}
