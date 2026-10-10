using RedAnts.DJ.Domain;
using RedAnts.DJ.Features.Board;

namespace RedAnts.DJ.Features.Admin;

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
