using System.Text.Json;
using Microsoft.Data.SqlClient;
using RedAnts.Features.Show;

namespace RedAnts.Infrastructure.Show;

public interface IShowProfileStore
{
    Task<IReadOnlyList<ShowProfile>> GetAllAsync();
    Task SaveAllAsync(IReadOnlyList<ShowProfile> profiles);
}

public sealed class ShowProfileStore(IConfiguration config) : IShowProfileStore
{
    private string? Dsn =>
        config.GetConnectionString("showDbDSN") is { Length: > 0 } s ? s : config.GetConnectionString("umbracoDbDSN");

    public async Task<IReadOnlyList<ShowProfile>> GetAllAsync()
    {
        var dsn = Dsn;
        if (string.IsNullOrWhiteSpace(dsn)) return ShowConfig.Profiles;

        await using var conn = new SqlConnection(dsn);
        await conn.OpenAsync();

        var list = new List<ShowProfile>();
        await using (var cmd = new SqlCommand("SELECT [Json] FROM [show].[Profiles] ORDER BY [SortOrder]", conn))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var profile = JsonSerializer.Deserialize<ShowProfile>(reader.GetString(0), ShowJson.Options);
                if (profile is not null) list.Add(profile);
            }
        }

        if (list.Count > 0) return list;

        await SaveAllInternalAsync(conn, ShowConfig.Profiles);
        return ShowConfig.Profiles;
    }

    public async Task SaveAllAsync(IReadOnlyList<ShowProfile> profiles)
    {
        var dsn = Dsn;
        if (string.IsNullOrWhiteSpace(dsn)) throw new InvalidOperationException("Keine Show-Datenbankverbindung konfiguriert.");
        await using var conn = new SqlConnection(dsn);
        await conn.OpenAsync();
        await SaveAllInternalAsync(conn, profiles);
    }

    private static async Task SaveAllInternalAsync(SqlConnection conn, IReadOnlyList<ShowProfile> profiles)
    {
        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync();
        await using (var del = new SqlCommand("DELETE FROM [show].[Profiles]", conn, tx))
            await del.ExecuteNonQueryAsync();

        for (var i = 0; i < profiles.Count; i++)
        {
            var p = profiles[i];
            var json = JsonSerializer.Serialize(p, ShowJson.Options);
            await using var ins = new SqlCommand(
                "INSERT INTO [show].[Profiles] ([Id],[SortOrder],[Json],[UpdatedAt]) VALUES (@id,@sort,@json,SYSUTCDATETIME())",
                conn, tx);
            ins.Parameters.AddWithValue("@id", p.Id);
            ins.Parameters.AddWithValue("@sort", i);
            ins.Parameters.AddWithValue("@json", json);
            await ins.ExecuteNonQueryAsync();
        }
        await tx.CommitAsync();
    }
}
