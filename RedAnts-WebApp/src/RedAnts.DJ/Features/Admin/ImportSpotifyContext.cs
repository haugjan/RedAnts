using RedAnts.DJ.Domain;

namespace RedAnts.DJ.Features.Admin;

public static class ImportSpotifyContext
{
    public sealed record Query(string Ref, int Max = 200);

    public sealed class Handler(IDJSpotifySearch spotify)
    {
        public Task<IReadOnlyList<SpotifyTrack>> HandleAsync(Query query) => spotify.GetContextTracksAsync(query.Ref, query.Max);
    }
}
