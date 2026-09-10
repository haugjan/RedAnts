namespace RedAnts.Show.Features.Admin;

public sealed record SpotifySettings(string SavedClientId, string EffectiveClientId, bool HasSecret, bool Configured, bool AccountConnected, string? AccountName);

public static class GetSpotifySettings
{
    public sealed record Query;

    public sealed class Handler(IShowSettings settings, IShowSpotifySearch spotify, IShowSpotifyAccount account, IConfiguration config)
    {
        public Task<SpotifySettings> HandleAsync(Query query)
        {
            var saved = settings.Get("Spotify:ClientId") ?? "";
            var effective = string.IsNullOrWhiteSpace(saved) ? config["Spotify:ClientId"] ?? "" : saved;
            var hasSecret = !string.IsNullOrWhiteSpace(settings.Get("Spotify:ClientSecret"));
            return Task.FromResult(new SpotifySettings(saved, effective, hasSecret, spotify.Configured, account.Connected, account.AccountName));
        }
    }
}
