using RedAnts.DJ.Domain;
using RedAnts.DJ.Features.Board;
using System.Text.Json;

namespace RedAnts.DJ.Infrastructure;

public sealed class DJProfileRepository(DJDatabase database) : IDJProfiles
{
    public async Task<IReadOnlyList<DJProfile>> GetAllAsync()
    {
        var rows = await database.RunAsync(db => db.FetchAsync<DJProfileRecord>(
            "SELECT [Id],[SortOrder],[Json],[UpdatedAt] FROM [show].[Profiles] ORDER BY [SortOrder]"));
        var stored = rows
            .Select(r => JsonSerializer.Deserialize<DJProfile>(r.Json, DJJson.Options))
            .OfType<DJProfile>()
            .ToList();
        var source = stored.Count > 0 ? stored : DJConfig.Profiles;
        var profiles = source.Select(DJProfileLayout.Upgrade).ToList();
        if (stored.Count == 0 || profiles.Where((p, i) => !ReferenceEquals(p, source[i])).Any())
            await SaveAllAsync(profiles);
        return profiles;
    }

    public Task SaveAllAsync(IReadOnlyList<DJProfile> profiles) => database.RunAsync(async db =>
    {
        await db.ExecuteAsync("DELETE FROM [show].[Profiles]");
        var now = DateTime.UtcNow;
        for (var i = 0; i < profiles.Count; i++)
            await db.InsertAsync(new DJProfileRecord
            {
                Id = profiles[i].Id,
                SortOrder = i,
                Json = JsonSerializer.Serialize(profiles[i], DJJson.Options),
                UpdatedAt = now
            });
    });
}
