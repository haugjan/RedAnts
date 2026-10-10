namespace RedAnts.DJ.Features.Admin;

public static class DisconnectSpotifyAccount
{
    public sealed record Command;

    public sealed class Handler(IDJSpotifyAccount account)
    {
        public Task HandleAsync(Command command) => account.DisconnectAsync();
    }
}
