
namespace RedAnts.Ticketing.Features.Helpers;

public static class RemoveHelperFromSeason
{
    public sealed record Command(int HelperId);

    public sealed class Handler(IHelperRepository helpers)
    {
        public Task HandleAsync(Command command) => helpers.DeleteAsync(command.HelperId);
    }
}
