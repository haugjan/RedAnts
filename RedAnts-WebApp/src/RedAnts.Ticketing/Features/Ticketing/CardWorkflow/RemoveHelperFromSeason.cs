using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.CardWorkflow;

public static class RemoveHelperFromSeason
{
    public sealed record Command(int HelperId);

    public sealed class Handler(IHelpers helpers)
    {
        public Task HandleAsync(Command command) => helpers.DeleteAsync(command.HelperId);
    }
}
