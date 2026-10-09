using System.Reflection;
using RedAnts.Game.Domain;
using RedAnts.Game.Features.Players;
using RedAnts.Game.Features.Settings;

namespace RedAnts.Game.Infrastructure.Content;

public static class GamePlayerSeeding
{
    private const string ResourceName = "RedAnts.Game.Infrastructure.Content.game-players-seed.json";

    public static async Task EnsureAsync(IGamePlayerRepository players, IGameSettings settings, ILogger logger)
    {
        if (await players.CountAsync() > 0) return;

        var json = await ReadAsync();
        if (json is null)
        {
            logger.LogWarning("Game seed data not found as an embedded resource; the market stays empty.");
            return;
        }

        try
        {
            var file = GamePlayerImportMapping.Parse(json);
            var imported = await players.ReplaceAllAsync(file.ToPlayers());

            var season = (file.Season ?? "").Trim();
            if (season.Length > 0) await settings.SetAsync(GameSettingKeys.Season, season);
            var asOf = (file.AsOf ?? "").Trim();
            if (asOf.Length > 0) await settings.SetAsync(GameSettingKeys.AsOf, asOf);

            logger.LogInformation("Seeded {Count} game players for season {Season}.", imported, season);
        }
        catch (DomainException ex)
        {
            logger.LogWarning(ex, "Game seed data could not be imported.");
        }
    }

    private static async Task<string?> ReadAsync()
    {
        await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
        if (stream is null) return null;
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
