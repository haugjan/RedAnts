using System.Collections.Concurrent;
using NPoco;
using RedAnts.Show.Features.Ports;

namespace RedAnts.Show.Infrastructure;

public sealed class ShowSettingsRepository(ShowDatabase database, ILogger<ShowSettingsRepository> logger) : IShowSettings
{
    private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);

    public string? Get(string key) => _cache.TryGetValue(key, out var value) && value.Length > 0 ? value : null;

    public async Task LoadAsync()
    {
        try
        {
            var rows = await database.RunAsync(db => db.FetchAsync<ShowSettingRecord>(
                "SELECT [Key],[Value],[UpdatedAt] FROM [show].[Settings]"));
            foreach (var row in rows)
                _cache[row.Key] = row.Value ?? "";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Show settings could not be loaded; the schema may not exist yet.");
        }
    }

    public async Task SetAsync(string key, string? value)
    {
        var stored = value ?? "";
        await database.RunAsync(async db =>
        {
            var now = DateTime.UtcNow;
            var updated = await db.ExecuteAsync(
                "UPDATE [show].[Settings] SET [Value] = @0, [UpdatedAt] = @1 WHERE [Key] = @2", stored, now, key);
            if (updated == 0)
                await db.InsertAsync(new ShowSettingRecord { Key = key, Value = stored, UpdatedAt = now });
        });
        _cache[key] = stored;
    }
}
