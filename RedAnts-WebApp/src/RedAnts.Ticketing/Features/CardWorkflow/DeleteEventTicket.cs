using RedAnts.Ticketing.Features.Admin;

namespace RedAnts.Ticketing.Features.CardWorkflow;

public static class DeleteEventTicket
{
    public sealed record Command(Guid Uuid);

    public sealed class Handler(IAdminTicketDeletion deletion)
    {
        public Task HandleAsync(Command command) => deletion.DeleteEventTicketAsync(command.Uuid);
    }
}
