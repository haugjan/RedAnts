using RedAnts.DJ.Domain;

namespace RedAnts.DJ.Features.Admin;

public interface IDJSpotifySearch
{
    bool Configured { get; }
    Task<IReadOnlyList<SpotifyTrack>> SearchAsync(string query, int limit = 10);
    Task<SpotifyTrack?> GetTrackAsync(string idOrUri);
    Task<SpotifyContext?> GetContextAsync(string idOrUri);
    Task<IReadOnlyList<SpotifyTrack>> GetContextTracksAsync(string idOrUri, int max = 200);
    Task<string> TestCredentialsAsync(string clientId, string secret);
}
