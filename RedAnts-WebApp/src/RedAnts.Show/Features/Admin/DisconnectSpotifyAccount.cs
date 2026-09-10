namespace RedAnts.Show.Features.Admin;

public static class DisconnectSpotifyAccount
{
    public sealed record Command;

    public sealed class Handler(IShowSpotifyAccount account)
    {
        public Task HandleAsync(Command command) => account.DisconnectAsync();
    }
}
