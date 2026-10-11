using System.Text.Json;
using NPoco;
using RedAnts.Show.Domain;
using RedAnts.Show.Infrastructure;

namespace RedAnts.Show.Features.Music.Infrastructure;

public sealed class StoredSounds(ShowDatabase database) : ISounds
{
    public async Task<IReadOnlyList<SoundCommand>> GetAllAsync()
    {
        var rows = await database.RunAsync(db => db.FetchAsync<SoundRecord>(
            "SELECT [Id],[SortOrder],[Json],[UpdatedAt] FROM [show].[Sounds] ORDER BY [SortOrder]"));
        return rows.Select(r => JsonSerializer.Deserialize<SoundCommand>(r.Json, ShowJson.Options)).OfType<SoundCommand>().ToList();
    }

    public Task SaveAllAsync(IReadOnlyList<SoundCommand> sounds) => database.RunAsync(async db =>
    {
        await db.ExecuteAsync("DELETE FROM [show].[Sounds]");
        var now = DateTime.UtcNow;
        for (var i = 0; i < sounds.Count; i++)
            await db.InsertAsync(new SoundRecord
            {
                Id = sounds[i].Id,
                SortOrder = i,
                Json = JsonSerializer.Serialize(sounds[i], ShowJson.Options),
                UpdatedAt = now,
            });
    });
}
