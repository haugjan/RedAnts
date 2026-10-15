using System.Text.Json;
using NPoco;
using RedAnts.Show.Domain;
using RedAnts.Show.Infrastructure;

namespace RedAnts.Show.Features.Games.Infrastructure;

public sealed class GameReader(ShowDatabase database) : IGameReader
{
    public async Task<IReadOnlyList<GameRow>> ListAsync()
    {
        var rows = await database.RunAsync(db => db.FetchAsync<GameRecord>(
            "SELECT [Id],[Json],[UpdatedAt] FROM [show].[Games] ORDER BY [UpdatedAt] DESC"));
        return rows
            .Select(r => JsonSerializer.Deserialize<Game>(r.Json, ShowJson.Options))
            .OfType<Game>()
            .Select(g => new GameRow(g.Id, g.Title, g.Date, g.Home.LongName, g.Away.LongName, g.Source, g.LoadedAt))
            .ToList();
    }
}
