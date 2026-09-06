using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;

namespace RedAnts.Infrastructure.Show;

public interface IShowSettingsStore
{
    string? Get(string key);
    Task SetAsync(string key, string? value);
    Task LoadAsync();
}

// Zentrale, in der DB gespeicherte Show-Einstellungen (z. B. Spotify-Client-ID/Secret),
// im UI editierbar. In-Memory-Cache für synchronen Lesezugriff.
public sealed class ShowSettingsStore(IConfiguration config) : IShowSettingsStore
{
    private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);
    private volatile bool _loaded;

    private string? Dsn =>
        config.GetConnectionString("showDbDSN") is { Length: > 0 } s ? s : config.GetConnectionString("umbracoDbDSN");

    public string? Get(string key) => _cache.TryGetValue(key, out var v) && v.Length > 0 ? v : null;

    public async Task LoadAsync()
    {
        var dsn = Dsn;
        if (string.IsNullOrWhiteSpace(dsn)) { _loaded = true; return; }
        try
        {
            await using var conn = new SqlConnection(dsn);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("SELECT [Key],[Value] FROM [show].[Settings]", conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var key = reader.GetString(0);
                var value = reader.IsDBNull(1) ? "" : reader.GetString(1);
                _cache[key] = value;
            }
        }
        catch { }
        _loaded = true;
    }

    public async Task SetAsync(string key, string? value)
    {
        var dsn = Dsn;
        if (string.IsNullOrWhiteSpace(dsn)) throw new InvalidOperationException("Keine Show-Datenbankverbindung konfiguriert.");
        var v = value ?? "";

        await using var conn = new SqlConnection(dsn);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("""
            MERGE [show].[Settings] AS t
            USING (SELECT @key AS [Key]) AS s ON t.[Key] = s.[Key]
            WHEN MATCHED THEN UPDATE SET [Value] = @value, [UpdatedAt] = SYSUTCDATETIME()
            WHEN NOT MATCHED THEN INSERT ([Key],[Value],[UpdatedAt]) VALUES (@key, @value, SYSUTCDATETIME());
            """, conn);
        cmd.Parameters.AddWithValue("@key", key);
        cmd.Parameters.AddWithValue("@value", (object?)v ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();

        _cache[key] = v;
    }
}
