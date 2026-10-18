using RedAnts.Show.Features.Agents;

namespace RedAnts.Show.Features.Music;

public static class DeleteMusicEntry
{
    public sealed record Command(string Id);

    public sealed class Handler(IDJProfiles profiles, AgentSync sync)
    {
        public async Task<IReadOnlyList<MusicEntry>> HandleAsync(Command command)
        {
            var library = GetMusicLibrary.Flatten(await profiles.GetAllAsync())
                .Where(b => b.Id != command.Id)
                .ToList();
            await MusicLibraryWriter.StoreAsync(profiles, library, sync);
            return GetMusicLibrary.Entries([MusicLibraryWriter.Library(library)]);
        }
    }
}
