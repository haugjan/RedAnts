namespace RedAnts.Show.Features.Music;

public static class SearchSounds
{
    public sealed record Query(string Term, string? Reference = null, int Limit = 10);

    public sealed class Handler(ISoundSearch search)
    {
        public Task<IReadOnlyList<SoundHit>> HandleAsync(Query query) =>
            search.FindAsync(query.Term, query.Reference, query.Limit);
    }
}
