using System.Text.Json;
using RedAnts.DJ.Domain;
using RedAnts.DJ.Features.Board;
using RedAnts.DJ.Features.Sounds;

namespace RedAnts.Show.Features.Music.Infrastructure;

public sealed class DJMusicLibrary(GetDJProfiles.Handler profiles) : IMusicLibrary
{
    public async Task<IReadOnlyList<MusicItem>> AllAsync()
    {
        var found = new List<MusicItem>();
        foreach (var profile in await profiles.HandleAsync(new GetDJProfiles.Query()))
            foreach (var button in profile.Root)
                Walk(button, profile.Name, found);
        return found;
    }

    public async Task<IReadOnlyList<MusicNode>> TreeAsync()
    {
        var roots = new List<MusicNode>();
        foreach (var profile in await profiles.HandleAsync(new GetDJProfiles.Query()))
            foreach (var button in profile.Root)
                if (Node(button) is { } node) roots.Add(node);
        return roots;
    }

    private static MusicNode? Node(DJButton button)
    {
        if (button.Panic) return null;
        if (button.IsControl)
            return Of(button, 0, "[]", []) with { Control = button.Control!.Value.ToString().ToLowerInvariant() };
        if (button.IsFolder)
        {
            var children = button.Children!.Select(Node).OfType<MusicNode>().ToList();
            return children.Count == 0 ? null : Of(button, 0, "[]", children);
        }
        var songs = button.EffectiveSongs.Where(s => s.Ref.Length > 0).ToList();
        return songs.Count == 0 ? null : Of(button, songs.Count, Json(songs), []);
    }

    private static MusicNode Of(DJButton button, int songs, string json, IReadOnlyList<MusicNode> children) =>
        new(button.Id, button.Label, button.Icon ?? "", button.Color ?? "", button.Subtitle ?? "",
            songs, button.SongsRandom, json, children, button.X, button.Y);

    private static void Walk(DJButton button, string folder, List<MusicItem> found)
    {
        if (button.IsFolder)
        {
            var below = button.Label.Length > 0 ? $"{folder} · {button.Label}" : folder;
            foreach (var child in button.Children!) Walk(child, below, found);
            return;
        }
        if (button.Panic || button.IsControl) return;
        var songs = button.EffectiveSongs.Where(s => s.Ref.Length > 0).ToList();
        if (songs.Count == 0) return;
        found.Add(new MusicItem(button.Id, button.Label, folder, songs.Count, button.SongsRandom, Json(songs),
            button.Icon ?? "", button.Color ?? "", button.Subtitle ?? ""));
    }

    private static string Json(IEnumerable<DJSound> songs) =>
        JsonSerializer.Serialize(songs.Select(x => new
        {
            t = x.Kind == SoundKind.Spotify ? "spotify" : "local",
            r = x.Kind == SoundKind.Spotify ? x.Ref : Url(x.Ref),
            s = x.StartSec,
            d = x.DurationSec,
            sh = x.Shuffle,
            ti = x.Title,
            ar = x.Artist,
        }));

    private static string Url(string reference) =>
        reference.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? reference : DJSoundDelivery.BoardBase + reference;
}
