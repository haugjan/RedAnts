using System.Text.Json;
using NPoco;
using RedAnts.Show.Domain;
using RedAnts.Show.Infrastructure;

namespace RedAnts.Show.Features.Halls.Infrastructure;

public sealed class HallRepository(ShowDatabase database) : IHallRepository
{
    public async Task<Hall?> GetByIdAsync(string id)
    {
        var row = await database.RunAsync(db => db.SingleOrDefaultAsync<HallRecord>(
            "SELECT [Id],[Json],[UpdatedAt] FROM [show].[Halls] WHERE [Id] = @0", id));
        return row is null ? null : JsonSerializer.Deserialize<Hall>(row.Json, ShowJson.Options);
    }

    public Task SaveAsync(Hall hall) => database.RunAsync(async db =>
    {
        var json = JsonSerializer.Serialize(hall, ShowJson.Options);
        var now = DateTime.UtcNow;
        var updated = await db.ExecuteAsync(
            "UPDATE [show].[Halls] SET [Json] = @0, [UpdatedAt] = @1 WHERE [Id] = @2", json, now, hall.Id);
        if (updated == 0)
            await db.InsertAsync(new HallRecord { Id = hall.Id, Json = json, UpdatedAt = now });
    });

    public Task DeleteAsync(string id) => database.RunAsync(db =>
        db.ExecuteAsync("DELETE FROM [show].[Halls] WHERE [Id] = @0", id));
}
