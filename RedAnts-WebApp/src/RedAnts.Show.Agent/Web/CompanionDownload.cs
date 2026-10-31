using System.Text.Json;
using System.Text.RegularExpressions;

namespace RedAnts.Show.Agent.Web;

public static class CompanionDownload
{
    public const string Path = "/bord/companion";
    const string Releases = "https://api.github.com/repos/haugjan/RedAnts/releases?per_page=50";
    const string Feed = "https://github.com/haugjan/RedAnts/releases.atom";
    const string Fallback = "https://github.com/haugjan/RedAnts/releases?q=halle-companion";
    const string TagPrefix = "halle-companion-v";
    static readonly TimeSpan Fresh = TimeSpan.FromMinutes(10);

    static readonly HttpClient Http = CreateClient();
    static readonly SemaphoreSlim Gate = new(1, 1);
    static (string Url, DateTimeOffset At)? latest;

    public static void MapCompanionDownload(this WebApplication app) =>
        app.MapGet(Path, async (CancellationToken stop) => Results.Redirect(await LatestAsync(stop) ?? Fallback));

    public static string? Pick(JsonElement releases)
    {
        foreach (var release in releases.EnumerateArray())
        {
            if (!release.TryGetProperty("tag_name", out var tag) || tag.GetString() is not { } name || !name.StartsWith(TagPrefix)) continue;
            if (IsTrue(release, "draft") || IsTrue(release, "prerelease")) continue;
            if (!release.TryGetProperty("assets", out var assets)) continue;
            foreach (var asset in assets.EnumerateArray())
            {
                if (asset.TryGetProperty("name", out var file) && file.GetString() is { } fileName && fileName.EndsWith(".tgz")
                    && asset.TryGetProperty("browser_download_url", out var url) && url.GetString() is { Length: > 0 } link)
                    return link;
            }
        }
        return null;
    }

    static async Task<string?> LatestAsync(CancellationToken stop)
    {
        await Gate.WaitAsync(stop);
        try
        {
            if (latest is { } known && DateTimeOffset.UtcNow - known.At < Fresh) return known.Url;
            var url = await FromFeedAsync(stop) ?? await FromApiAsync(stop);
            if (url is not null) latest = (url, DateTimeOffset.UtcNow);
            return url ?? latest?.Url;
        }
        finally
        {
            Gate.Release();
        }
    }

    public static string? PickFromFeed(string atom)
    {
        var match = Regex.Match(atom, @"releases/tag/halle-companion-v(\d[\d.]*\d)");
        return match.Success
            ? $"https://github.com/haugjan/RedAnts/releases/download/{TagPrefix}{match.Groups[1].Value}/redants-halle-{match.Groups[1].Value}.tgz"
            : null;
    }

    static async Task<string?> FromFeedAsync(CancellationToken stop)
    {
        try
        {
            var url = PickFromFeed(await Http.GetStringAsync(Feed, stop));
            if (url is null) return null;
            using var head = await Http.SendAsync(new HttpRequestMessage(HttpMethod.Head, url), stop);
            return head.IsSuccessStatusCode ? url : null;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return null;
        }
    }

    static async Task<string?> FromApiAsync(CancellationToken stop)
    {
        try
        {
            await using var body = await Http.GetStreamAsync(Releases, stop);
            using var json = await JsonDocument.ParseAsync(body, cancellationToken: stop);
            return Pick(json.RootElement);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException)
        {
            return null;
        }
    }

    static bool IsTrue(JsonElement release, string name) =>
        release.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;

    static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("RedAnts-ShowAgent");
        return client;
    }
}
