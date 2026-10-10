using System.Text.Json;
using NPoco;
using RedAnts.Show.Domain;
using RedAnts.Show.Infrastructure;

namespace RedAnts.Show.Features.Scenes.Infrastructure;

public sealed class StoredScenes(ShowDatabase database) : IScenes
{
    public async Task<IReadOnlyList<Scene>> GetAllAsync()
    {
        var rows = await database.RunAsync(db => db.FetchAsync<SceneRecord>(
            "SELECT [Id],[SortOrder],[Json],[UpdatedAt] FROM [show].[Scenes] ORDER BY [SortOrder]"));
        return rows.Select(r => JsonSerializer.Deserialize<Scene>(r.Json, ShowJson.Options)).OfType<Scene>().ToList();
    }

    public Task SaveAllAsync(IReadOnlyList<Scene> scenes) => database.RunAsync(async db =>
    {
        await db.ExecuteAsync("DELETE FROM [show].[Scenes]");
        var now = DateTime.UtcNow;
        for (var i = 0; i < scenes.Count; i++)
            await db.InsertAsync(new SceneRecord
            {
                Id = scenes[i].Id,
                SortOrder = i,
                Json = JsonSerializer.Serialize(scenes[i], ShowJson.Options),
                UpdatedAt = now,
            });
    });
}
