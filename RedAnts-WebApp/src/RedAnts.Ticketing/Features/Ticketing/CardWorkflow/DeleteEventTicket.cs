using RedAnts.Features.Ticketing.Admin;

namespace RedAnts.Features.Ticketing.CardWorkflow;

public static class DeleteEventTicket
{
    public sealed record Command(Guid Uuid);

    public sealed class Handler(IAdminTicketDeletion deletion)
    {
        public Task HandleAsync(Command command) => deletion.DeleteEventTicketAsync(command.Uuid);
    }
}
