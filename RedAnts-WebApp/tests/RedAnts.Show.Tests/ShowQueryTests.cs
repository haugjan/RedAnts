using Microsoft.Extensions.Configuration;
using RedAnts.Domain;
using RedAnts.Show.Domain;
using RedAnts.Show.Features.Admin;
using RedAnts.Show.Features.Remote;
using RedAnts.Show.Features.Sounds;
using Xunit;

namespace RedAnts.Show.Tests;

public class ShowQueryTests
{
    [Fact]
    public async Task GetSpotifySettings_reports_saved_and_effective_client_id_and_secret_presence()
    {
        var settings = new StubSettings { ["Spotify:ClientSecret"] = "s3cret" };
        var config = new ConfigurationBuilder().AddInMemoryCollection([new("Spotify:ClientId", "from-config")]).Build();
        var handler = new GetSpotifySettings.Handler(settings, new StubSpotify { Configured = true }, new StubSpotifyAccount(), config);

        var result = await handler.HandleAsync(new GetSpotifySettings.Query());

        Assert.Equal("", result.SavedClientId);
        Assert.Equal("from-config", result.EffectiveClientId);
        Assert.True(result.HasSecret);
        Assert.True(result.Configured);
        Assert.False(result.AccountConnected);
    }

    [Fact]
    public async Task ImportSpotifyContext_returns_the_tracks_of_the_reference()
    {
        var handler = new ImportSpotifyContext.Handler(new StubSpotify { Configured = true });

        var tracks = await handler.HandleAsync(new ImportSpotifyContext.Query("spotify:playlist:abc", 5));

        Assert.Equal(["spotify:track:1", "spotify:track:2"], tracks.Select(t => t.Uri));
    }

    [Fact]
    public async Task DisconnectSpotifyAccount_drops_the_stored_connection()
    {
        var account = new StubSpotifyAccount { Connected = true };
        var handler = new DisconnectSpotifyAccount.Handler(account);

        await handler.HandleAsync(new DisconnectSpotifyAccount.Command());

        Assert.False(account.Connected);
    }

    [Fact]
    public async Task TestSpotifyCredentials_falls_back_to_the_saved_secret()
    {
        var spotify = new StubSpotify();
        var handler = new TestSpotifyCredentials.Handler(new StubSettings { ["Spotify:ClientSecret"] = "saved" }, spotify);

        var result = await handler.HandleAsync(new TestSpotifyCredentials.Command(" id ", " "));

        Assert.Equal("ok", result);
        Assert.Equal(("id", "saved"), spotify.Tested);
    }

    [Fact]
    public async Task LookupSpotifyReference_resolves_tracks_and_contexts_only_when_configured()
    {
        var spotify = new StubSpotify { Configured = true };
        var handler = new LookupSpotifyReference.Handler(spotify);

        var track = await handler.HandleAsync(new LookupSpotifyReference.Query("spotify:track:4cOdK2wGLETKBW3PvgPWqT"));
        var context = await handler.HandleAsync(new LookupSpotifyReference.Query("spotify:playlist:37i9dQZF1DXcBWIGoYBM5M"));
        var unconfigured = await new LookupSpotifyReference.Handler(new StubSpotify())
            .HandleAsync(new LookupSpotifyReference.Query("spotify:track:4cOdK2wGLETKBW3PvgPWqT"));

        Assert.Equal("Track", track.Track?.Name);
        Assert.Null(track.Context);
        Assert.Equal("Context", context.Context?.Name);
        Assert.Null(context.Track);
        Assert.Equal(SpotifyReference.Unknown, unconfigured);
    }

    [Fact]
    public async Task DownloadShowSound_returns_the_stored_bytes()
    {
        var uploader = new RecordingUploader { Stored = { ["sounds/a.mp3"] = [1, 2, 3] } };
        var handler = new DownloadShowSound.Handler(uploader);

        Assert.Equal([1, 2, 3], await handler.HandleAsync(new DownloadShowSound.Query("sounds/a.mp3")));
        Assert.Null(await handler.HandleAsync(new DownloadShowSound.Query(" ")));
    }

    [Fact]
    public async Task RestoreShowSound_uploads_at_the_given_path_and_rejects_blank_paths()
    {
        var uploader = new RecordingUploader();
        var handler = new RestoreShowSound.Handler(uploader);

        await handler.HandleAsync(new RestoreShowSound.Command("sounds/b.mp3", new MemoryStream([9]), "audio/mpeg"));

        Assert.Equal(("sounds/b.mp3", "audio/mpeg"), Assert.Single(uploader.Restored));
        await Assert.ThrowsAsync<DomainException>(() =>
            handler.HandleAsync(new RestoreShowSound.Command(" ", new MemoryStream(), null)));
    }

    [Fact]
    public async Task OpenShowSound_refuses_paths_that_leave_the_container()
    {
        var uploader = new RecordingUploader { Stored = { ["sounds/c.mp3"] = [1] } };
        var handler = new OpenShowSound.Handler(uploader);

        Assert.NotNull(await handler.HandleAsync(new OpenShowSound.Query("sounds/c.mp3")));
        Assert.Null(await handler.HandleAsync(new OpenShowSound.Query("../sounds/c.mp3")));
        Assert.Null(await handler.HandleAsync(new OpenShowSound.Query("/sounds/c.mp3")));
        Assert.Null(await handler.HandleAsync(new OpenShowSound.Query(null)));
    }

    [Fact]
    public async Task ListenForShowCommands_registers_the_board_for_its_room()
    {
        var remote = new RecordingRemote();
        var handler = new ListenForShowCommands.Handler(remote);

        using var registration = await handler.HandleAsync(new ListenForShowCommands.Command("hall", _ => Task.CompletedTask));

        Assert.Equal("hall", remote.RegisteredRoom);
    }

    private sealed class StubSettings : IShowSettings
    {
        private readonly Dictionary<string, string?> _values = new();

        public string? this[string key]
        {
            set => _values[key] = value;
        }

        public string? Get(string key) => _values.GetValueOrDefault(key);

        public Task SetAsync(string key, string? value)
        {
            _values[key] = value;
            return Task.CompletedTask;
        }

        public Task LoadAsync() => Task.CompletedTask;
    }

    private sealed class StubSpotify : IShowSpotifySearch
    {
        public bool Configured { get; init; }
        public (string ClientId, string Secret)? Tested { get; private set; }

        public Task<IReadOnlyList<SpotifyTrack>> SearchAsync(string query, int limit = 10) => Task.FromResult<IReadOnlyList<SpotifyTrack>>([]);

        public Task<SpotifyTrack?> GetTrackAsync(string idOrUri) => Task.FromResult<SpotifyTrack?>(new SpotifyTrack(idOrUri, "Track", "Artist"));

        public Task<SpotifyContext?> GetContextAsync(string idOrUri) => Task.FromResult<SpotifyContext?>(new SpotifyContext(idOrUri, "playlist", "Context"));

        public Task<IReadOnlyList<SpotifyTrack>> GetContextTracksAsync(string idOrUri, int max = 200) =>
            Task.FromResult<IReadOnlyList<SpotifyTrack>>(
            [
                new SpotifyTrack("spotify:track:1", "One", "Artist"),
                new SpotifyTrack("spotify:track:2", "Two", "Artist"),
            ]);

        public Task<string> TestCredentialsAsync(string clientId, string secret)
        {
            Tested = (clientId, secret);
            return Task.FromResult("ok");
        }
    }

    private sealed class StubSpotifyAccount : IShowSpotifyAccount
    {
        public bool Connected { get; set; }
        public string? AccountName => Connected ? "Agent" : null;

        public string BuildAuthorizeUrl(string redirectUri, string state) => $"https://accounts.spotify.com/authorize?state={state}";

        public Task<string?> AccessTokenAsync() => Task.FromResult<string?>(Connected ? "token" : null);

        public Task<string> CompleteAsync(string code, string redirectUri)
        {
            Connected = true;
            return Task.FromResult("Agent");
        }

        public Task DisconnectAsync()
        {
            Connected = false;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingUploader : IShowSoundUploader
    {
        public Dictionary<string, byte[]> Stored { get; } = new();
        public List<(string Path, string? ContentType)> Restored { get; } = [];

        public Task<string> UploadAsync(string fileName, Stream content, string? contentType) => Task.FromResult("sounds/" + fileName);

        public Task UploadAtPathAsync(string blobPath, Stream content, string? contentType)
        {
            Restored.Add((blobPath, contentType));
            return Task.CompletedTask;
        }

        public Task<byte[]?> DownloadAsync(string blobPath) => Task.FromResult(Stored.GetValueOrDefault(blobPath));

        public Task<ShowSoundContent?> OpenReadAsync(string blobPath) =>
            Task.FromResult(Stored.TryGetValue(blobPath, out var bytes)
                ? new ShowSoundContent(new MemoryStream(bytes), "audio/mpeg", null, null)
                : null);
    }

    private sealed class RecordingRemote : IShowRemote
    {
        public string? RegisteredRoom { get; private set; }

        public IDisposable Register(string? room, Func<ShowCommand, Task> handler)
        {
            RegisteredRoom = room;
            return new Registration();
        }

        public Task<int> DispatchAsync(ShowCommand command) => Task.FromResult(0);

        private sealed class Registration : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
