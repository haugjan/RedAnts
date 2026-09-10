namespace RedAnts.Show.Features.Admin;

public interface IShowSpotifyAccount
{
    bool Connected { get; }
    string? AccountName { get; }
    string BuildAuthorizeUrl(string redirectUri, string state);
    Task<string> CompleteAsync(string code, string redirectUri);
    Task<string?> AccessTokenAsync();
    Task DisconnectAsync();
}
