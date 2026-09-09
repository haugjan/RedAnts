using RedAnts.Show.Domain;
using RedAnts.Show.Features.Ports;

namespace RedAnts.Show.Features.ShowWorkflow;

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
