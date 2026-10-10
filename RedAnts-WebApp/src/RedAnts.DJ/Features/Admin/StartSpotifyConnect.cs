namespace RedAnts.DJ.Features.Admin;

public static class StartSpotifyConnect
{
    public sealed record Query(string RedirectUri, string State);

    public sealed class Handler(IDJSpotifyAccount account)
    {
        public Task<string> HandleAsync(Query query) =>
            Task.FromResult(account.BuildAuthorizeUrl(query.RedirectUri, query.State));
    }
}
