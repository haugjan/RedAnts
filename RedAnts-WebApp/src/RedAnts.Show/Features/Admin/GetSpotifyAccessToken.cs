namespace RedAnts.Show.Features.Admin;

public static class GetSpotifyAccessToken
{
    public sealed record Query;

    public sealed class Handler(IShowSpotifyAccount account)
    {
        public async Task<string?> HandleAsync(Query query)
        {
            if (!account.Connected) return null;
            var token = await account.AccessTokenAsync();
            return string.IsNullOrEmpty(token) ? null : token;
        }
    }
}
