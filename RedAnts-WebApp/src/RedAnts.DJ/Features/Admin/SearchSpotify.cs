using RedAnts.DJ.Domain;

namespace RedAnts.DJ.Features.Admin;

public static class SearchSpotify
{
    public sealed record Query(string Term, int Limit = 10);

    public sealed class Handler(IDJSpotifySearch spotify)
    {
        public Task<IReadOnlyList<SpotifyTrack>> HandleAsync(Query query) => spotify.SearchAsync(query.Term, query.Limit);
    }
}
