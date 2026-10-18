using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Music;

public static class GetMusicLibrary
{
    public const string LibraryId = "bibliothek";
    public const string LibraryName = "Bibliothek";

    public sealed record Query;

    public sealed class Handler(IDJProfiles profiles)
    {
        public async Task<IReadOnlyList<MusicEntry>> HandleAsync(Query query) =>
            Entries(await profiles.GetAllAsync());
    }

    public static IReadOnlyList<MusicEntry> Entries(IReadOnlyList<DJProfile> profiles) =>
        Flatten(profiles)
            .Select(MusicEntry.Of)
            .OrderBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    public static IReadOnlyList<DJButton> Flatten(IReadOnlyList<DJProfile> profiles)
    {
        var flat = new List<DJButton>();
        foreach (var profile in profiles)
            foreach (var button in profile.Root)
                Walk(button, profiles.Count > 1 ? profile.Name : "", flat);
        return flat;
    }

    private static void Walk(DJButton button, string path, List<DJButton> flat)
    {
        if (button.Panic || button.IsControl) return;
        var name = Join(path, button.Label);
        if (button.IsFolder)
        {
            foreach (var child in button.Children!) Walk(child, name, flat);
            return;
        }
        if (button.EffectiveSongs.Count == 0) return;
        flat.Add(new DJButton(button.Id, name, Songs: button.EffectiveSongs, SongsRandom: button.SongsRandom));
    }

    private static string Join(string path, string label) =>
        path.Length == 0 ? label.Trim() : label.Trim().Length == 0 ? path : $"{path} · {label.Trim()}";
}
