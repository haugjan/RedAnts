using RedAnts.Show.Domain;
using RedAnts.Show.Features.Music;

namespace RedAnts.Show.Features.Admin;

public static class SaveDJProfiles
{
    public sealed record Command(IReadOnlyList<DJProfile> Profiles);

    public sealed class Handler(IDJProfiles profiles)
    {
        public async Task HandleAsync(Command command)
        {
            DJProfileRules.Validate(command.Profiles);
            await profiles.SaveAllAsync(command.Profiles);
        }
    }
}
