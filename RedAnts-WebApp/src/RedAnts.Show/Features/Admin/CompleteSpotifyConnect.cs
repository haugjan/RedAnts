namespace RedAnts.Show.Features.Admin;

public static class CompleteSpotifyConnect
{
    public sealed record Command(string Code, string RedirectUri);

    public sealed class Handler(IShowSpotifyAccount account)
    {
        public Task<string> HandleAsync(Command command) => account.CompleteAsync(command.Code, command.RedirectUri);
    }
}
