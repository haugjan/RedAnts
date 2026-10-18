using RedAnts.Show.Domain;
using RedAnts.Show.Features.Agents;

namespace RedAnts.Show.Features.Music;

public static class SaveMusicEntry
{
    public sealed record Command(MusicEntry Entry);

    public sealed class Handler(IDJProfiles profiles, AgentSync sync)
    {
        public async Task<IReadOnlyList<MusicEntry>> HandleAsync(Command command)
        {
            if (command.Entry.Name.Trim().Length == 0)
                throw new DomainException("Ein Name ist erforderlich.");

            var library = GetMusicLibrary.Flatten(await profiles.GetAllAsync()).ToList();
            var index = library.FindIndex(b => b.Id == command.Entry.Id);
            if (index < 0) library.Add(command.Entry.ToButton());
            else library[index] = command.Entry.ToButton();

            await MusicLibraryWriter.StoreAsync(profiles, library, sync);
            return GetMusicLibrary.Entries([MusicLibraryWriter.Library(library)]);
        }
    }
}
