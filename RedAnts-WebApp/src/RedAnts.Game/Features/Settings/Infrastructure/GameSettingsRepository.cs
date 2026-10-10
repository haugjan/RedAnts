using System.Collections.Concurrent;
using NPoco;
using RedAnts.Game.Domain;
using RedAnts.Game.Infrastructure;

namespace RedAnts.Game.Features.Settings.Infrastructure;

public sealed class GameSettingsRepository(GameDatabase database, ILogger<GameSettingsRepository> logger) : IGameSettings
{
    private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);

    public string? Get(string key) =>
        _cache.TryGetValue(key, out var value) && value.Length > 0 ? value : null;

    public int Budget =>
        int.TryParse(Get(GameSettingKeys.Budget), out var budget) && budget > 0
            ? budget
            : Domain.Squad.DefaultBudget;

    public int Round =>
        int.TryParse(Get(GameSettingKeys.Round), out var round) && round > 0 ? round : 1;

    public async Task LoadAsync()
    {
        try
        {
            var rows = await database.RunAsync(db => db.FetchAsync<GameSettingRecord>(
                "SELECT [Key],[Value],[UpdatedAt] FROM [game].[Settings]"));
            foreach (var row in rows)
                _cache[row.Key] = row.Value ?? "";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Game settings could not be loaded; the schema may not exist yet.");
        }
    }

    public async Task SetAsync(string key, string? value)
    {
        var stored = value ?? "";
        await database.RunAsync(async db =>
        {
            var now = SwissTime.Timestamp;
            var updated = await db.ExecuteAsync(
                "UPDATE [game].[Settings] SET [Value] = @0, [UpdatedAt] = @1 WHERE [Key] = @2", stored, now, key);
            if (updated == 0)
                await db.InsertAsync(new GameSettingRecord { Key = key, Value = stored, UpdatedAt = now });
        });
        _cache[key] = stored;
    }
}
