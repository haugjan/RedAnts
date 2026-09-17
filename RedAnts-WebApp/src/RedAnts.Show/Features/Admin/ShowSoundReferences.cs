using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Admin;

internal static class ShowSoundReferences
{
    public static HashSet<string> Collect(IReadOnlyList<ShowProfile> profiles)
    {
        var acc = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in profiles) Walk(p.Root, acc);
        return acc;
    }

    private static void Walk(IReadOnlyList<ShowButton> nodes, HashSet<string> acc)
    {
        foreach (var n in nodes)
        {
            Add(acc, n.Sound);
            if (n.Songs is { } songs) foreach (var s in songs) Add(acc, s);
            if (n.Pool is { } pool) foreach (var s in pool) Add(acc, s);
            if (n.Children is { } children) Walk(children, acc);
        }
    }

    private static void Add(HashSet<string> acc, ShowSound? sound)
    {
        if (sound is { Kind: SoundKind.Local } && !string.IsNullOrWhiteSpace(sound.Ref))
            acc.Add(sound.Ref);
    }
}
