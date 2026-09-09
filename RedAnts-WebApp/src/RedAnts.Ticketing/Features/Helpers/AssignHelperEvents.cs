
namespace RedAnts.Ticketing.Features.Helpers;

public static class AssignHelperEvents
{
    public sealed record Command(int HelperId, bool AllEvents, IReadOnlyList<int> EventIds, bool CanRebook);

    public sealed class Handler(IHelperRepository helpers)
    {
        public Task HandleAsync(Command command) =>
            helpers.SetAssignmentAsync(command.HelperId, command.AllEvents, command.EventIds, command.CanRebook);
    }
}
