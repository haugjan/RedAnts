using System.Text.Json;
using NPoco;
using RedAnts.Show.Domain;
using RedAnts.Show.Infrastructure;

namespace RedAnts.Show.Features.States.Infrastructure;

public sealed class StoredShowStates(ShowDatabase database) : IShowStates
{
    public async Task<IReadOnlyList<ShowState>> GetAllAsync()
    {
        var rows = await database.RunAsync(db => db.FetchAsync<StateRecord>(
            "SELECT [Id],[SortOrder],[Json],[UpdatedAt] FROM [show].[States] ORDER BY [SortOrder]"));
        return rows.Select(r => JsonSerializer.Deserialize<ShowState>(r.Json, ShowJson.Options)).OfType<ShowState>().ToList();
    }

    public Task SaveAllAsync(IReadOnlyList<ShowState> states) => database.RunAsync(async db =>
    {
        await db.ExecuteAsync("DELETE FROM [show].[States]");
        var now = DateTime.UtcNow;
        for (var i = 0; i < states.Count; i++)
            await db.InsertAsync(new StateRecord
            {
                Id = states[i].Id,
                SortOrder = i,
                Json = JsonSerializer.Serialize(states[i], ShowJson.Options),
                UpdatedAt = now,
            });
    });
}
