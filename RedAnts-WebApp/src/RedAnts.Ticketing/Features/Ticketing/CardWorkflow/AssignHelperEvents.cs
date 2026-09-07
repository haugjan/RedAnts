using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.CardWorkflow;

public static class AssignHelperEvents
{
    public sealed record Command(int HelperId, bool AllEvents, IReadOnlyList<int> EventIds, bool CanRebook);

    public sealed class Handler(IHelpers helpers)
    {
        public Task HandleAsync(Command command) =>
            helpers.SetAssignmentAsync(command.HelperId, command.AllEvents, command.EventIds, command.CanRebook);
    }
}
