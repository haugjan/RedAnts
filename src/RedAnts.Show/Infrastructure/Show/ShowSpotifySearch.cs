using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace RedAnts.Infrastructure.Show;

public sealed record SpotifyTrack(string Uri, string Name, string Artist);

public interface IShowSpotifySearch
{
    bool Configured { get; }
    Task<IReadOnlyList<SpotifyTrack>> SearchAsync(string query, int limit = 10);
}

// Server-seitige Spotify-Suche via Client-Credentials (kein User-Login nötig).
// Erfordert Spotify:ClientId und Spotify:ClientSecret in der Konfiguration.
public sealed class ShowSpotifySearch(IHttpClientFactory httpFactory, IConfiguration config) : IShowSpotifySearch
{
    private string? ClientId => config["Spotify:ClientId"];
    private string? Secret => config["Spotify:ClientSecret"];
    public bool Configured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(Secret);

    private string? _token;
    private DateTime _expiresUtc;

    private async Task<string> TokenAsync()
    {
        if (_token is not null && DateTime.UtcNow < _expiresUtc) return _token;
        var client = httpFactory.CreateClient();
        var req = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["grant_type"] = "client_credentials" }),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ClientId?.Trim()}:{Secret?.Trim()}")));
        var res = await client.SendAsync(req);
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Token {(int)res.StatusCode}: {err}");
        }
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        _token = json.GetProperty("access_token").GetString();
        _expiresUtc = DateTime.UtcNow.AddSeconds(json.GetProperty("expires_in").GetInt32() - 60);
        return _token!;
    }

    public async Task<IReadOnlyList<SpotifyTrack>> SearchAsync(string query, int limit = 10)
    {
        if (!Configured || string.IsNullOrWhiteSpace(query)) return [];
        limit = Math.Clamp(limit, 1, 10);
        var token = await TokenAsync();
        var client = httpFactory.CreateClient();
        var url = $"https://api.spotify.com/v1/search?type=track&limit={limit}&market=CH&q={Uri.EscapeDataString(query.Trim())}";
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await client.SendAsync(req);
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Search {(int)res.StatusCode}: {err}");
        }
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        var items = json.GetProperty("tracks").GetProperty("items");
        var list = new List<SpotifyTrack>();
        foreach (var t in items.EnumerateArray())
        {
            var uri = t.GetProperty("uri").GetString() ?? "";
            var name = t.GetProperty("name").GetString() ?? "";
            var artist = t.TryGetProperty("artists", out var a) && a.GetArrayLength() > 0
                ? a[0].GetProperty("name").GetString() ?? "" : "";
            if (uri.Length > 0) list.Add(new SpotifyTrack(uri, name, artist));
        }
        return list;
    }
}
