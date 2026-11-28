#:package Microsoft.Data.SqlClient@6.1.1
using Microsoft.Data.SqlClient;

var db = args.Length > 0 ? args[0] : "sqldb-redants-prod";
var outDir = Path.GetFullPath(Path.Combine(Environment.GetEnvironmentVariable("DUMP_DIR") ?? ".", "dump-" + db));
Directory.CreateDirectory(outDir);
var cs = $"Server=tcp:sql-redants-ch.database.windows.net,1433;Database={db};Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;";
await using var con = new SqlConnection(cs);
await con.OpenAsync();
Console.WriteLine($"connected {db}, writing to {outDir}");

static string Safe(string s) => string.Concat(s.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_'));

await using (var cmd = new SqlCommand("SELECT [Key], [Value], [UpdatedAt] FROM show.Settings ORDER BY [Key]", con))
await using (var r = await cmd.ExecuteReaderAsync())
{
    while (await r.ReadAsync())
    {
        var key = r.GetString(0);
        var value = r.IsDBNull(1) ? "" : r.GetString(1);
        Console.WriteLine($"setting {key} len={value.Length} updated={r.GetDateTime(2):yyyy-MM-dd HH:mm}");
        if (key.StartsWith("Show:", StringComparison.OrdinalIgnoreCase))
            await File.WriteAllTextAsync(Path.Combine(outDir, "setting-" + Safe(key) + ".json"), value);
    }
}

await using (var cmd = new SqlCommand("SELECT Id, Json, UpdatedAt FROM show.Halls ORDER BY Id", con))
await using (var r = await cmd.ExecuteReaderAsync())
{
    while (await r.ReadAsync())
    {
        var id = r.GetString(0);
        var json = r.GetString(1);
        Console.WriteLine($"hall {id} len={json.Length} updated={r.GetDateTime(2):yyyy-MM-dd HH:mm}");
        await File.WriteAllTextAsync(Path.Combine(outDir, "hall-" + Safe(id) + ".json"), json);
    }
}

await using (var cmd = new SqlCommand("SELECT Id, SortOrder, Json, UpdatedAt FROM show.Profiles ORDER BY SortOrder", con))
await using (var r = await cmd.ExecuteReaderAsync())
{
    while (await r.ReadAsync())
    {
        var id = r.GetString(0);
        var json = r.GetString(2);
        Console.WriteLine($"profile {id} sort={r.GetInt32(1)} len={json.Length} updated={r.GetDateTime(3):yyyy-MM-dd HH:mm}");
        await File.WriteAllTextAsync(Path.Combine(outDir, "profile-" + Safe(id) + ".json"), json);
    }
}

await using (var cmd = new SqlCommand("SELECT Id, UpdatedAt, LEN(Json) FROM show.Games ORDER BY UpdatedAt DESC", con))
await using (var r = await cmd.ExecuteReaderAsync())
{
    var n = 0;
    while (await r.ReadAsync())
    {
        n++;
        if (n <= 12) Console.WriteLine($"game {r.GetString(0)} updated={r.GetDateTime(1):yyyy-MM-dd HH:mm} len={r.GetInt64(2)}");
    }
    Console.WriteLine($"games total {n}");
}
