using RedAnts.Show.Domain;
using RedAnts.Show.Features.Board;
using System.Text.Json;

namespace RedAnts.Show.Infrastructure;

public sealed class ShowProfileRepository(ShowDatabase database) : IShowProfiles
{
    public async Task<IReadOnlyList<ShowProfile>> GetAllAsync()
    {
        var rows = await database.RunAsync(db => db.FetchAsync<ShowProfileRecord>(
            "SELECT [Id],[SortOrder],[Json],[UpdatedAt] FROM [show].[Profiles] ORDER BY [SortOrder]"));
        var stored = rows
            .Select(r => JsonSerializer.Deserialize<ShowProfile>(r.Json, ShowJson.Options))
            .OfType<ShowProfile>()
            .ToList();
        var source = stored.Count > 0 ? stored : ShowConfig.Profiles;
        var profiles = source.Select(ShowProfileLayout.Upgrade).ToList();
        if (stored.Count == 0 || profiles.Where((p, i) => !ReferenceEquals(p, source[i])).Any())
            await SaveAllAsync(profiles);
        return profiles;
    }

    public Task SaveAllAsync(IReadOnlyList<ShowProfile> profiles) => database.RunAsync(async db =>
    {
        await db.ExecuteAsync("DELETE FROM [show].[Profiles]");
        var now = DateTime.UtcNow;
        for (var i = 0; i < profiles.Count; i++)
            await db.InsertAsync(new ShowProfileRecord
            {
                Id = profiles[i].Id,
                SortOrder = i,
                Json = JsonSerializer.Serialize(profiles[i], ShowJson.Options),
                UpdatedAt = now
            });
    });
}
