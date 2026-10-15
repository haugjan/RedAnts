using System.Text.Json;
using NPoco;
using RedAnts.Show.Domain;
using RedAnts.Show.Infrastructure;

namespace RedAnts.Show.Features.Games.Infrastructure;

public sealed class GameRepository(ShowDatabase database) : IGameRepository
{
    public async Task<Game?> GetByIdAsync(string id)
    {
        var row = await database.RunAsync(db => db.SingleOrDefaultAsync<GameRecord>(
            "SELECT [Id],[Json],[UpdatedAt] FROM [show].[Games] WHERE [Id] = @0", id));
        return row is null ? null : JsonSerializer.Deserialize<Game>(row.Json, ShowJson.Options);
    }

    public Task SaveAsync(Game game) => database.RunAsync(async db =>
    {
        var json = JsonSerializer.Serialize(game, ShowJson.Options);
        var now = DateTime.UtcNow;
        var updated = await db.ExecuteAsync(
            "UPDATE [show].[Games] SET [Json] = @0, [UpdatedAt] = @1 WHERE [Id] = @2", json, now, game.Id);
        if (updated == 0)
            await db.InsertAsync(new GameRecord { Id = game.Id, Json = json, UpdatedAt = now });
    });

    public Task DeleteAsync(string id) => database.RunAsync(db =>
        db.ExecuteAsync("DELETE FROM [show].[Games] WHERE [Id] = @0", id));
}
