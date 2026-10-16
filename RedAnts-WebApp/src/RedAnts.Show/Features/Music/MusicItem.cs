namespace RedAnts.Show.Features.Music;

public sealed record MusicItem(
    string Id,
    string Name,
    string Folder,
    int Songs,
    bool Random,
    string SongsJson = "[]",
    string Icon = "",
    string Color = "",
    string Subtitle = "")
{
    public string Display => Folder.Length > 0 ? $"{Folder} · {Name}" : Name;

    public bool IsPlaylist => Songs > 1;
}

public sealed record MusicNode(
    string Id,
    string Name,
    string Icon,
    string Color,
    string Subtitle,
    int Songs,
    bool Random,
    string SongsJson,
    IReadOnlyList<MusicNode> Children,
    int X = -1,
    int Y = -1,
    string Control = "")
{
    public bool IsFolder => Children.Count > 0;
}
