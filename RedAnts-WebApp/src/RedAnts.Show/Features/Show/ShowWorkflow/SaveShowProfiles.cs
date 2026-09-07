using RedAnts.Domain.Show;
using RedAnts.Features.Show.Ports;

namespace RedAnts.Features.Show.ShowWorkflow;

public static class SaveShowProfiles
{
    public sealed record Command(IReadOnlyList<ShowProfile> Profiles);

    public sealed class Handler(IShowProfiles profiles)
    {
        public async Task HandleAsync(Command command)
        {
            ShowProfileRules.Validate(command.Profiles);
            await profiles.SaveAllAsync(command.Profiles);
        }
    }
}
