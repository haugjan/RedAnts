using RedAnts.Show.Domain;
using RedAnts.Show.Features.Agents;

namespace RedAnts.Show.Features.Music;

public static class MusicLibraryWriter
{
    public static DJProfile Library(IReadOnlyList<DJButton> buttons) =>
        new(GetMusicLibrary.LibraryId, GetMusicLibrary.LibraryName, null, buttons);

    public static async Task StoreAsync(IDJProfiles profiles, IReadOnlyList<DJButton> buttons, AgentSync sync)
    {
        await profiles.SaveAllAsync([Library(buttons)]);
        await sync.PushAllAsync();
    }
}
