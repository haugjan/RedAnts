using RedAnts.Show.Domain;
using RedAnts.Show.Features.Agents;

namespace RedAnts.Show.Features.Music;

public static class ImportMusicLibrary
{
    public sealed record Command(IReadOnlyList<MusicEntry> Entries, bool Replace);

    public sealed record Result(int Added, int Updated, int Total);

    public sealed class Handler(IDJProfiles profiles, AgentSync sync)
    {
        public async Task<Result> HandleAsync(Command command)
        {
            var wanted = command.Entries
                .Where(entry => entry.Name.Trim().Length > 0)
                .ToList();
            if (wanted.Count == 0) throw new DomainException("Die Datei enthält keine Einträge.");

            var library = command.Replace
                ? []
                : GetMusicLibrary.Flatten(await profiles.GetAllAsync()).ToList();

            var added = 0;
            var updated = 0;
            foreach (var entry in wanted)
            {
                var id = entry.Id.Trim().Length > 0 ? entry.Id.Trim() : Guid.NewGuid().ToString("N")[..12];
                var button = (entry with { Id = id }).ToButton();
                var index = library.FindIndex(known => known.Id == id);
                if (index < 0) { library.Add(button); added++; }
                else { library[index] = button; updated++; }
            }

            await MusicLibraryWriter.StoreAsync(profiles, library, sync);
            return new Result(added, updated, library.Count);
        }
    }
}
