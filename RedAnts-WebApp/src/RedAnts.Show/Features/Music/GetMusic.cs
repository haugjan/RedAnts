namespace RedAnts.Show.Features.Music;

public static class GetMusic
{
    public sealed record Query;

    public sealed class Handler(IMusicLibrary library)
    {
        public Task<IReadOnlyList<MusicItem>> HandleAsync(Query query) => library.AllAsync();
    }
}
