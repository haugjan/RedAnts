using System.Text.Json;

namespace RedAnts.Show.Agent.Web;

public static class CompanionDownload
{
    public const string Path = "/bord/companion";
    const string Releases = "https://api.github.com/repos/haugjan/RedAnts/releases?per_page=50";
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
            try
            {
                await using var body = await Http.GetStreamAsync(Releases, stop);
                using var json = await JsonDocument.ParseAsync(body, cancellationToken: stop);
                var url = Pick(json.RootElement);
                if (url is not null) latest = (url, DateTimeOffset.UtcNow);
                return url;
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException)
            {
                return latest?.Url;
            }
        }
        finally
        {
            Gate.Release();
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
