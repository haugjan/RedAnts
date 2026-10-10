using RedAnts.DJ.Domain;

namespace RedAnts.DJ.Features.Admin;

public sealed record SpotifyReference(SpotifyTrack? Track, SpotifyContext? Context)
{
    public static readonly SpotifyReference Unknown = new(null, null);
}

public static class LookupSpotifyReference
{
    public sealed record Query(string Reference);

    public sealed class Handler(IDJSpotifySearch spotify)
    {
        public async Task<SpotifyReference> HandleAsync(Query query)
        {
            if (!spotify.Configured || DJSpotifyLink.Parse(query.Reference) is not { } parsed) return SpotifyReference.Unknown;
            return parsed.Kind == "track"
                ? new SpotifyReference(await spotify.GetTrackAsync(query.Reference), null)
                : new SpotifyReference(null, await spotify.GetContextAsync(query.Reference));
        }
    }
}
