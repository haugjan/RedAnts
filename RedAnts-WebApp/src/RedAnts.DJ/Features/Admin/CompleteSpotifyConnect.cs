namespace RedAnts.DJ.Features.Admin;

public static class CompleteSpotifyConnect
{
    public sealed record Command(string Code, string RedirectUri);

    public sealed class Handler(IDJSpotifyAccount account)
    {
        public Task<string> HandleAsync(Command command) => account.CompleteAsync(command.Code, command.RedirectUri);
    }
}
