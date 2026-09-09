using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Admin;

public static class SearchSpotify
{
    public sealed record Query(string Term, int Limit = 10);

    public sealed class Handler(IShowSpotifySearch spotify)
    {
        public Task<IReadOnlyList<SpotifyTrack>> HandleAsync(Query query) => spotify.SearchAsync(query.Term, query.Limit);
    }
}
