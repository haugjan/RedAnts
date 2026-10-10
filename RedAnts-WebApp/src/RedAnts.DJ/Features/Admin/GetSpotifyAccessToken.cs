namespace RedAnts.DJ.Features.Admin;

public static class GetSpotifyAccessToken
{
    public sealed record Query;

    public sealed class Handler(IDJSpotifyAccount account)
    {
        public async Task<string?> HandleAsync(Query query)
        {
            if (!account.Connected) return null;
            var token = await account.AccessTokenAsync();
            return string.IsNullOrEmpty(token) ? null : token;
        }
    }
}
