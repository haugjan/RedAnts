using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Admin;

public static class LookupSpotifyTracks
{
    public sealed record Query(IReadOnlyCollection<string> References);

    public sealed class Handler(IShowSpotifySearch spotify)
    {
        public async Task<IReadOnlyDictionary<string, SpotifyTrack>> HandleAsync(Query query)
        {
            if (!spotify.Configured || query.References.Count == 0)
                return new Dictionary<string, SpotifyTrack>(StringComparer.OrdinalIgnoreCase);
            return await spotify.GetTracksAsync(query.References);
        }
    }
}
