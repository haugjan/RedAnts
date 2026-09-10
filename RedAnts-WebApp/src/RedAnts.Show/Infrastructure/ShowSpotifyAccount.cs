using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using RedAnts.Show.Features.Admin;

namespace RedAnts.Show.Infrastructure;

public sealed class ShowSpotifyAccount(
    IHttpClientFactory httpFactory,
    IConfiguration config,
    IShowSettings settings,
    ILogger<ShowSpotifyAccount> logger) : IShowSpotifyAccount
{
    private const string RefreshKey = "Spotify:RefreshToken";
    private const string NameKey = "Spotify:AccountName";
    private const string Scopes = "playlist-read-private playlist-read-collaborative user-read-private "
        + "streaming user-read-email user-modify-playback-state user-read-playback-state";

    private string? ClientId => settings.Get("Spotify:ClientId") ?? config["Spotify:ClientId"];
    private string? Secret => settings.Get("Spotify:ClientSecret") ?? config["Spotify:ClientSecret"];

    public bool Connected => !string.IsNullOrWhiteSpace(settings.Get(RefreshKey));
    public string? AccountName => settings.Get(NameKey);

    private string? _access;
    private DateTime _expiresUtc;

    public string BuildAuthorizeUrl(string redirectUri, string state)
    {
        var query = new (string Key, string Value)[]
        {
            ("client_id", ClientId?.Trim() ?? ""),
            ("response_type", "code"),
            ("redirect_uri", redirectUri),
            ("scope", Scopes),
            ("state", state),
            ("show_dialog", "true"),
        };
        var q = string.Join("&", query.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
        return "https://accounts.spotify.com/authorize?" + q;
    }

    public async Task<string> CompleteAsync(string code, string redirectUri)
    {
        var json = await PostTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
        });

        var refresh = json.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
        if (string.IsNullOrWhiteSpace(refresh)) throw new InvalidOperationException("Spotify hat keinen Refresh-Token geliefert.");
        CacheAccess(json);
        await settings.SetAsync(RefreshKey, refresh);

        var name = await ReadAccountNameAsync();
        await settings.SetAsync(NameKey, name);
        return name;
    }

    public async Task DisconnectAsync()
    {
        _access = null;
        _expiresUtc = DateTime.MinValue;
        await settings.SetAsync(RefreshKey, "");
        await settings.SetAsync(NameKey, "");
    }

    public async Task<string?> AccessTokenAsync()
    {
        if (!Connected) return null;
        if (_access is not null && DateTime.UtcNow < _expiresUtc) return _access;

        var refresh = settings.Get(RefreshKey);
        if (string.IsNullOrWhiteSpace(refresh)) return null;
        try
        {
            var json = await PostTokenAsync(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refresh,
            });
            if (json.TryGetProperty("refresh_token", out var rt) && rt.GetString() is { Length: > 0 } rotated)
                await settings.SetAsync(RefreshKey, rotated);
            CacheAccess(json);
            return _access;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Spotify refresh token rejected; the account connection was dropped.");
            await DisconnectAsync();
            return null;
        }
    }

    private void CacheAccess(JsonElement json)
    {
        _access = json.GetProperty("access_token").GetString();
        _expiresUtc = DateTime.UtcNow.AddSeconds(json.GetProperty("expires_in").GetInt32() - 60);
    }

    private async Task<JsonElement> PostTokenAsync(Dictionary<string, string> form)
    {
        if (string.IsNullOrWhiteSpace(ClientId) || string.IsNullOrWhiteSpace(Secret))
            throw new InvalidOperationException("Client-ID und Client-Secret fehlen.");

        var client = httpFactory.CreateClient();
        var req = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token")
        {
            Content = new FormUrlEncodedContent(form),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ClientId.Trim()}:{Secret.Trim()}")));
        var res = await client.SendAsync(req);
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Spotify-Token {(int)res.StatusCode}: {err}");
        }
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<string> ReadAccountNameAsync()
    {
        try
        {
            var client = httpFactory.CreateClient();
            var req = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _access);
            var res = await client.SendAsync(req);
            if (!res.IsSuccessStatusCode) return "Spotify-Konto";
            var me = await res.Content.ReadFromJsonAsync<JsonElement>();
            var display = me.TryGetProperty("display_name", out var dn) ? dn.GetString() : null;
            if (!string.IsNullOrWhiteSpace(display)) return display;
            return me.TryGetProperty("id", out var id) ? id.GetString() ?? "Spotify-Konto" : "Spotify-Konto";
        }
        catch { return "Spotify-Konto"; }
    }
}
