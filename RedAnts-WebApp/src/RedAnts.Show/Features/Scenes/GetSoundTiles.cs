namespace RedAnts.Show.Features.Scenes;

public sealed record SoundTile(string Id, string Label, string? Icon, string? Color, string ProfileId, string ProfileName, string Path)
{
    public string Target => $"tile:{ProfileId}/{Id}";

    public string Display => Path.Length > 0 ? $"{Label} · {ProfileName} / {Path}" : $"{Label} · {ProfileName}";
}

public static class GetSoundTiles
{
    public sealed record Query;

    public sealed class Handler(ISoundLibrary library)
    {
        public Task<IReadOnlyList<SoundTile>> HandleAsync(Query query) => library.ListAsync();
    }
}
