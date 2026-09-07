using RedAnts.Features.Ticketing.Admin;

namespace RedAnts.Features.Ticketing.CardWorkflow;

public static class DeleteMemberCard
{
    public sealed record Command(Guid Uuid);

    public sealed class Handler(IAdminTicketDeletion deletion)
    {
        public Task HandleAsync(Command command) => deletion.DeleteMemberCardAsync(command.Uuid);
    }
}
