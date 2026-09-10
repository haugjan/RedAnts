using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Admin;

public interface IShowSpotifySearch
{
    bool Configured { get; }
    Task<IReadOnlyList<SpotifyTrack>> SearchAsync(string query, int limit = 10);
    Task<SpotifyTrack?> GetTrackAsync(string idOrUri);
    Task<SpotifyContext?> GetContextAsync(string idOrUri);
    Task<IReadOnlyList<SpotifyTrack>> GetContextTracksAsync(string idOrUri, int max = 200);
    Task<string> TestCredentialsAsync(string clientId, string secret);
}
