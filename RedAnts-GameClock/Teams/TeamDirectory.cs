using System.Text.Json;
using RedAnts.GameClock.Configuration;

namespace RedAnts.GameClock.Teams;

public sealed record DirectoryTeam(int Id, string Name, string? LogoUrl);

public sealed record LeagueOption(int GameClass, string Label);

public sealed class TeamDirectory(IHttpClientFactory clients, DataFolder data, ILogger<TeamDirectory> log)
{
    public const int LuplLeague = 24;

    public static readonly LeagueOption[] Leagues =
    [
        new(21, "Damen L-UPL"),
        new(11, "Herren L-UPL"),
    ];

    static readonly JsonSerializerOptions Format = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public async Task<IReadOnlyList<DirectoryTeam>> LoadAsync(int season, int gameClass, bool refresh, CancellationToken stop)
    {
        var cache = data.Cache($"teams-{season}-{gameClass}.json");
        if (!refresh && File.Exists(cache))
        {
            try
            {
                var cached = JsonSerializer.Deserialize<List<DirectoryTeam>>(await File.ReadAllTextAsync(cache, stop), Format);
                if (cached is { Count: > 0 }) return cached;
            }
            catch (JsonException) { }
        }

        var teams = await FetchAsync(season, gameClass, stop);
        if (teams.Count > 0) await File.WriteAllTextAsync(cache, JsonSerializer.Serialize(teams, Format), stop);
        return teams;
    }

    async Task<List<DirectoryTeam>> FetchAsync(int season, int gameClass, CancellationToken stop)
    {
        var url = $"https://api-v2.swissunihockey.ch/api/teams?season={season}&league={LuplLeague}&game_class={gameClass}";
        using var response = await clients.CreateClient(nameof(TeamDirectory)).GetAsync(url, stop);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(stop));
        var teams = new List<DirectoryTeam>();
        if (!document.RootElement.TryGetProperty("data", out var payload)) return teams;
        if (!payload.TryGetProperty("regions", out var regions)) return teams;

        foreach (var region in regions.EnumerateArray())
            foreach (var row in region.GetProperty("rows").EnumerateArray())
            {
                var cells = row.GetProperty("cells");
                var name = cells[0].GetProperty("text")[0].GetString() ?? "";
                var logo = cells.GetArrayLength() > 1 && cells[1].TryGetProperty("image", out var image)
                    ? image.GetProperty("url").GetString()
                    : null;
                if (name.Length > 0) teams.Add(new DirectoryTeam(row.GetProperty("id").GetInt32(), name, logo));
            }

        log.LogInformation("{Count} Teams aus der swissunihockey-API geladen (Saison {Season}, Spielklasse {GameClass})", teams.Count, season, gameClass);
        return teams.OrderBy(t => t.Name, StringComparer.CurrentCulture).ToList();
    }

    public async Task<string> CacheLogoAsync(DirectoryTeam team, CancellationToken stop)
    {
        if (string.IsNullOrWhiteSpace(team.LogoUrl)) return "";

        var extension = Path.GetExtension(new Uri(team.LogoUrl).AbsolutePath);
        if (extension.Length < 2) extension = ".png";

        var name = $"team-{team.Id}{extension}";
        var file = Path.Combine(data.Logos, name);
        if (!File.Exists(file))
        {
            var bytes = await clients.CreateClient(nameof(TeamDirectory)).GetByteArrayAsync(team.LogoUrl, stop);
            await File.WriteAllBytesAsync(file, bytes, stop);
        }
        return LogoFiles.Url(name);
    }
}

public static class LogoFiles
{
    public const string Route = "/teamlogo";

    public static string Url(string file) => $"{Route}/{file}";

    public static bool IsStored(string logo) => logo.StartsWith(Route + "/", StringComparison.OrdinalIgnoreCase);

    public static string? FileOf(string logo) =>
        IsStored(logo) ? Path.GetFileName(logo.AsSpan(Route.Length + 1).ToString()) : null;

    public static string ContentType(string file) => Path.GetExtension(file).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".svg" => "image/svg+xml",
        ".gif" => "image/gif",
        _ => "application/octet-stream",
    };
}
