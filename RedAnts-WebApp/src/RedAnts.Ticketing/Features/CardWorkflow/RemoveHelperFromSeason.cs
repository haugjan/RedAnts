using RedAnts.Ticketing.Features.Ports;

namespace RedAnts.Ticketing.Features.CardWorkflow;

public static class RemoveHelperFromSeason
{
    public sealed record Command(int HelperId);

    public sealed class Handler(IHelpers helpers)
    {
        public Task HandleAsync(Command command) => helpers.DeleteAsync(command.HelperId);
    }
}
