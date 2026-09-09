using RedAnts.Domain.Show;

namespace RedAnts.Features.Show.Ports;

public interface IShowProfiles
{
    Task<IReadOnlyList<ShowProfile>> GetAllAsync();
    Task SaveAllAsync(IReadOnlyList<ShowProfile> profiles);
}

public interface IShowSettings
{
    string? Get(string key);
    Task SetAsync(string key, string? value);
    Task LoadAsync();
}

public interface IShowRemote
{
    IDisposable Register(string? room, Func<ShowCommand, Task> handler);
    Task<int> DispatchAsync(ShowCommand command);
}

public interface IShowSpotifySearch
{
    bool Configured { get; }
    Task<IReadOnlyList<SpotifyTrack>> SearchAsync(string query, int limit = 10);
    Task<SpotifyTrack?> GetTrackAsync(string idOrUri);
    Task<SpotifyContext?> GetContextAsync(string idOrUri);
    Task<IReadOnlyList<SpotifyTrack>> GetContextTracksAsync(string idOrUri, int max = 200);
    Task<string> TestCredentialsAsync(string clientId, string secret);
}

public interface IShowSpotifyAccount
{
    bool Connected { get; }
    string? AccountName { get; }
    string BuildAuthorizeUrl(string redirectUri, string state);
    Task<string> CompleteAsync(string code, string redirectUri);
    Task DisconnectAsync();
}

public sealed record ShowSoundContent(Stream Content, string ContentType, DateTimeOffset? LastModified, string? ETag);

public interface IShowSoundUploader
{
    Task<string> UploadAsync(string fileName, Stream content, string? contentType);
    Task UploadAtPathAsync(string blobPath, Stream content, string? contentType);
    Task<byte[]?> DownloadAsync(string blobPath);
    Task<ShowSoundContent?> OpenReadAsync(string blobPath);
}
