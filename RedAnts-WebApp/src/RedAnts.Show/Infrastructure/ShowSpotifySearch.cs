using RedAnts.Show.Domain;
using RedAnts.Show.Features.Admin;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RedAnts.Show.Infrastructure;

public sealed class ShowSpotifySearch(
    IHttpClientFactory httpFactory,
    IConfiguration config,
    IShowSettings settings,
    ShowSpotifyAccount account) : IShowSpotifySearch
{
    private string? ClientId => settings.Get("Spotify:ClientId") ?? config["Spotify:ClientId"];
    private string? Secret => settings.Get("Spotify:ClientSecret") ?? config["Spotify:ClientSecret"];
    public bool Configured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(Secret);

    private string? _token;
    private DateTime _expiresUtc;

    public async Task<string> TestCredentialsAsync(string clientId, string secret)
    {
        if (string.IsNullOrWhiteSpace(clientId)) return "Client-ID fehlt.";
        if (string.IsNullOrWhiteSpace(secret)) return "Client-Secret fehlt (für den Test nötig).";
        try
        {
            var client = httpFactory.CreateClient();
            var req = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["grant_type"] = "client_credentials" }),
            };
            req.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId.Trim()}:{secret.Trim()}")));
            var res = await client.SendAsync(req);
            if (res.IsSuccessStatusCode) return "ok";
            var err = await res.Content.ReadAsStringAsync();
            return $"Fehler {(int)res.StatusCode}: {err}";
        }
        catch (Exception ex) { return "Fehler: " + ex.Message; }
    }

    private async Task<string> TokenAsync()
    {
        if (await account.UserTokenAsync() is { Length: > 0 } userToken) return userToken;
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
            "playlist" => $"https://api.spotify.com/v1/playlists/{v.Id}",
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
            var count = TrackTotal(json);
            return new SpotifyContext($"spotify:{v.Kind}:{v.Id}", v.Kind, name, owner, cover, count);
        }
        catch { return new SpotifyContext($"spotify:{v.Kind}:{v.Id}", v.Kind, "", "", "", 0); }
    }

    public async Task<IReadOnlyList<SpotifyTrack>> GetContextTracksAsync(string idOrUri, int max = 200)
    {
        if (!Configured) return [];
        if (ShowSpotifyLink.Parse(idOrUri) is not { } v || v.Kind == "track") return [];
        max = Math.Clamp(max, 1, 500);

        return v.Kind switch
        {
            "playlist" => await PlaylistTracksAsync(v.Id, max),
            "album" => await AlbumTracksAsync(v.Id, max),
            "artist" => await ArtistTopTracksAsync(v.Id, max),
            _ => [],
        };
    }

    private async Task<IReadOnlyList<SpotifyTrack>> PlaylistTracksAsync(string id, int max)
    {
        if (!account.Connected)
            throw new InvalidOperationException(
                "Spotify gibt Playlist-Inhalte nur an ein verbundenes Konto heraus. Im Dialog „🟢 Spotify\" einmalig „Mit Spotify verbinden\" ausführen.");

        var tracks = new List<SpotifyTrack>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var url = $"https://api.spotify.com/v1/playlists/{id}?market=CH";
        var firstPage = true;

        while (url.Length > 0 && tracks.Count < max)
        {
            JsonElement json;
            try { json = await GetAsync(url); }
            catch (HttpRequestException) when (!firstPage) { break; }
            firstPage = false;

            var (items, next) = ReadTrackPage(json);
            if (items.ValueKind != JsonValueKind.Array) break;

            foreach (var item in items.EnumerateArray())
            {
                if (item.TryGetProperty("is_local", out var loc) && loc.ValueKind == JsonValueKind.True) continue;
                if (EntryTrack(item) is not { } t) continue;
                if (t.TryGetProperty("type", out var ty) && ty.GetString() != "track") continue;
                if (MapTrack(t) is not { } track || !track.Uri.StartsWith("spotify:track:", StringComparison.Ordinal)) continue;
                if (!seen.Add(track.Uri)) continue;
                tracks.Add(track);
                if (tracks.Count >= max) break;
            }
            url = next;
        }
        return tracks;
    }

    private static int TrackTotal(JsonElement json)
    {
        foreach (var key in new[] { "tracks", "items" })
        {
            if (json.TryGetProperty(key, out var node) && node.ValueKind == JsonValueKind.Object
                && node.TryGetProperty("total", out var total) && total.ValueKind == JsonValueKind.Number)
                return total.GetInt32();
        }
        return 0;
    }

    private static (JsonElement Items, string Next) ReadTrackPage(JsonElement json)
    {
        var node = json;
        if (json.TryGetProperty("tracks", out var tracks) && tracks.ValueKind == JsonValueKind.Object) node = tracks;
        else if (json.TryGetProperty("items", out var embedded) && embedded.ValueKind == JsonValueKind.Object) node = embedded;

        var items = node.TryGetProperty("items", out var arr) && arr.ValueKind == JsonValueKind.Array ? arr : default;
        var next = node.TryGetProperty("next", out var nx) && nx.ValueKind == JsonValueKind.String ? nx.GetString() ?? "" : "";
        return (items, next);
    }

    private static JsonElement? EntryTrack(JsonElement item)
    {
        if (item.TryGetProperty("track", out var t) && t.ValueKind == JsonValueKind.Object) return t;
        if (item.TryGetProperty("item", out var i) && i.ValueKind == JsonValueKind.Object) return i;
        return item.TryGetProperty("uri", out _) ? item : null;
    }

    private async Task<IReadOnlyList<SpotifyTrack>> AlbumTracksAsync(string id, int max)
    {
        var album = await GetAsync($"https://api.spotify.com/v1/albums/{id}?market=CH");
        var albumName = album.TryGetProperty("name", out var an) ? an.GetString() ?? "" : "";
        var cover = FirstImage(album);

        var tracks = new List<SpotifyTrack>();
        var url = $"https://api.spotify.com/v1/albums/{id}/tracks?market=CH&limit=50";
        while (url.Length > 0 && tracks.Count < max)
        {
            var json = await GetAsync(url);
            foreach (var t in json.GetProperty("items").EnumerateArray())
            {
                if (MapTrack(t) is not { } track || !track.Uri.StartsWith("spotify:track:", StringComparison.Ordinal)) continue;
                tracks.Add(track with { Album = albumName, CoverUrl = cover });
                if (tracks.Count >= max) break;
            }
            url = json.TryGetProperty("next", out var nx) && nx.ValueKind == JsonValueKind.String ? nx.GetString() ?? "" : "";
        }
        return tracks;
    }

    private async Task<IReadOnlyList<SpotifyTrack>> ArtistTopTracksAsync(string id, int max)
    {
        var json = await GetAsync($"https://api.spotify.com/v1/artists/{id}/top-tracks?market=CH");
        var tracks = new List<SpotifyTrack>();
        foreach (var t in json.GetProperty("tracks").EnumerateArray())
        {
            if (MapTrack(t) is not { } track) continue;
            tracks.Add(track);
            if (tracks.Count >= max) break;
        }
        return tracks;
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
