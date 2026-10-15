namespace RedAnts.Show.Features.Music;

public sealed record MusicItem(
    string Id,
    string Name,
    string Folder,
    int Songs,
    bool Random,
    string SongsJson = "[]")
{
    public string Display => Folder.Length > 0 ? $"{Folder} · {Name}" : Name;

    public bool IsPlaylist => Songs > 1;
}
