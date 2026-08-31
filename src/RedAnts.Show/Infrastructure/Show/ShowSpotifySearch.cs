using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RedAnts.Infrastructure.Show;

public sealed record SpotifyTrack(
    string Uri,
    string Name,
    string Artist,
    string Album = "",
    string CoverUrl = "",
    string? PreviewUrl = null,
    int DurationMs = 0);

public sealed record SpotifyContext(
    string Uri,
    string Kind,
    string Name,
    string Owner = "",
    string CoverUrl = "",
    int TrackCount = 0);

public interface IShowSpotifySearch
{
    bool Configured { get; }
    Task<IReadOnlyList<SpotifyTrack>> SearchAsync(string query, int limit = 10);
    Task<SpotifyTrack?> GetTrackAsync(string idOrUri);
    Task<SpotifyContext?> GetContextAsync(string idOrUri);
}

public static partial class ShowSpotifyLink
{
    public static (string Kind, string Id)? Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var s = input.Trim();

        var uri = UriPattern().Match(s);
        if (uri.Success) return (uri.Groups[1].Value, uri.Groups[2].Value);

        var url = UrlPattern().Match(s);
        if (url.Success) return (url.Groups[1].Value, url.Groups[2].Value);

        if (BareId().IsMatch(s)) return ("track", s);
        return null;
    }

    public static string? ToUri(string? input)
    {
        var p = Parse(input);
        return p is { } v ? $"spotify:{v.Kind}:{v.Id}" : null;
    }

    [GeneratedRegex(@"^spotify:(track|playlist|album|artist|episode|show):([A-Za-z0-9]+)")]
    private static partial Regex UriPattern();

    [GeneratedRegex(@"open\.spotify\.com/(?:intl-[a-z-]+/)?(track|playlist|album|artist|episode|show)/([A-Za-z0-9]+)")]
    private static partial Regex UrlPattern();

    [GeneratedRegex(@"^[A-Za-z0-9]{22}$")]
    private static partial Regex BareId();
}

public sealed class ShowSpotifySearch(IHttpClientFactory httpFactory, IConfiguration config, IShowSettingsStore settings) : IShowSpotifySearch
{
    private string? ClientId => settings.Get("Spotify:ClientId") ?? config["Spotify:ClientId"];
    private string? Secret => settings.Get("Spotify:ClientSecret") ?? config["Spotify:ClientSecret"];
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

    private async Task<JsonElement> GetAsync(string url)
    {
        var token = await TokenAsync();
        var client = httpFactory.CreateClient();
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await client.SendAsync(req);
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Spotify {(int)res.StatusCode}: {err}");
        }
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    public async Task<IReadOnlyList<SpotifyTrack>> SearchAsync(string query, int limit = 10)
    {
        if (!Configured || string.IsNullOrWhiteSpace(query)) return [];
        limit = Math.Clamp(limit, 1, 10);
        var url = $"https://api.spotify.com/v1/search?type=track&limit={limit}&market=CH&q={Uri.EscapeDataString(query.Trim())}";
        var json = await GetAsync(url);
        var items = json.GetProperty("tracks").GetProperty("items");
        var list = new List<SpotifyTrack>();
        foreach (var t in items.EnumerateArray())
        {
            var track = MapTrack(t);
            if (track is not null) list.Add(track);
        }
        return list;
    }

    public async Task<SpotifyTrack?> GetTrackAsync(string idOrUri)
    {
        if (!Configured) return null;
        var p = ShowSpotifyLink.Parse(idOrUri);
        if (p is not { } v || v.Kind != "track") return null;
        var json = await GetAsync($"https://api.spotify.com/v1/tracks/{v.Id}?market=CH");
        return MapTrack(json);
    }

    public async Task<SpotifyContext?> GetContextAsync(string idOrUri)
    {
        if (!Configured) return null;
        var p = ShowSpotifyLink.Parse(idOrUri);
        if (p is not { } v) return null;
        if (v.Kind == "track") return null;

        var endpoint = v.Kind switch
        {
            "playlist" => $"https://api.spotify.com/v1/playlists/{v.Id}?fields=name,owner(display_name),images,tracks(total)",
            "album" => $"https://api.spotify.com/v1/albums/{v.Id}",
            "artist" => $"https://api.spotify.com/v1/artists/{v.Id}",
            _ => null,
        };
        if (endpoint is null) return null;

        try
        {
            var json = await GetAsync(endpoint);
            var name = json.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
            var cover = FirstImage(json);
            var owner = json.TryGetProperty("owner", out var o) && o.TryGetProperty("display_name", out var dn) ? dn.GetString() ?? "" : "";
            var count = json.TryGetProperty("tracks", out var tr) && tr.TryGetProperty("total", out var tot) ? tot.GetInt32() : 0;
            return new SpotifyContext($"spotify:{v.Kind}:{v.Id}", v.Kind, name, owner, cover, count);
        }
        catch { return new SpotifyContext($"spotify:{v.Kind}:{v.Id}", v.Kind, "", "", "", 0); }
    }

    private static SpotifyTrack? MapTrack(JsonElement t)
    {
        var uri = t.TryGetProperty("uri", out var u) ? u.GetString() ?? "" : "";
        if (uri.Length == 0) return null;
        var name = t.TryGetProperty("name", out var nm) ? nm.GetString() ?? "" : "";
        var artist = t.TryGetProperty("artists", out var a) && a.GetArrayLength() > 0
            ? a[0].GetProperty("name").GetString() ?? "" : "";
        var album = "";
        var cover = "";
        if (t.TryGetProperty("album", out var al))
        {
            album = al.TryGetProperty("name", out var an) ? an.GetString() ?? "" : "";
            cover = FirstImage(al);
        }
        var preview = t.TryGetProperty("preview_url", out var pv) && pv.ValueKind == JsonValueKind.String ? pv.GetString() : null;
        var dur = t.TryGetProperty("duration_ms", out var d) ? d.GetInt32() : 0;
        return new SpotifyTrack(uri, name, artist, album, cover, preview, dur);
    }

    private static string FirstImage(JsonElement node)
    {
        if (node.TryGetProperty("images", out var imgs) && imgs.ValueKind == JsonValueKind.Array && imgs.GetArrayLength() > 0)
            return imgs[0].GetProperty("url").GetString() ?? "";
        return "";
    }
}
