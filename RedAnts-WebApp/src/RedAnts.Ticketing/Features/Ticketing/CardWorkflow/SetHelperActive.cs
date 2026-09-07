using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.CardWorkflow;

public static class SetHelperActive
{
    public sealed record Command(int HelperId, bool Active);

    public sealed class Handler(IHelpers helpers)
    {
        public Task HandleAsync(Command command) => helpers.SetActiveAsync(command.HelperId, command.Active);
    }
}
