namespace RedAnts.DJ.Features.Admin;

public static class TestSpotifyCredentials
{
    public sealed record Command(string ClientId, string? Secret);

    public sealed class Handler(IDJSettings settings, IDJSpotifySearch spotify)
    {
        public Task<string> HandleAsync(Command command)
        {
            var secret = string.IsNullOrWhiteSpace(command.Secret)
                ? settings.Get("Spotify:ClientSecret") ?? ""
                : command.Secret.Trim();
            return spotify.TestCredentialsAsync(command.ClientId.Trim(), secret);
        }
    }
}
