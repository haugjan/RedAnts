using System.Text.Json;
using NPoco;
using RedAnts.Show.Domain;
using RedAnts.Show.Features;
using RedAnts.Show.Features.Ports;

namespace RedAnts.Show.Infrastructure;

public sealed class ShowProfileRepository(ShowDatabase database) : IShowProfiles
{
    public async Task<IReadOnlyList<ShowProfile>> GetAllAsync()
    {
        var rows = await database.RunAsync(db => db.FetchAsync<ShowProfileRecord>(
            "SELECT [Id],[SortOrder],[Json],[UpdatedAt] FROM [show].[Profiles] ORDER BY [SortOrder]"));
        var profiles = rows
            .Select(r => JsonSerializer.Deserialize<ShowProfile>(r.Json, ShowJson.Options))
            .OfType<ShowProfile>()
            .ToList();
        if (profiles.Count > 0) return profiles;

        await SaveAllAsync(ShowConfig.Profiles);
        return ShowConfig.Profiles;
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
