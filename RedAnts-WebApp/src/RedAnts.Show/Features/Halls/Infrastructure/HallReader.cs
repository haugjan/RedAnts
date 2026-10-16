using System.Text.Json;
using NPoco;
using RedAnts.Show.Domain;
using RedAnts.Show.Infrastructure;

namespace RedAnts.Show.Features.Halls.Infrastructure;

public sealed class HallReader(ShowDatabase database) : IHallReader
{
    private static bool MusicOnly(Hall hall) =>
        hall.Offers is { Sound: true, Clock: false, Vmix: false, Led: false };

    public async Task<IReadOnlyList<HallRow>> ListAsync()
    {
        var rows = await database.RunAsync(db => db.FetchAsync<HallRecord>(
            "SELECT [Id],[Json],[UpdatedAt] FROM [show].[Halls] ORDER BY [Id]"));
        return rows
            .Select(r => JsonSerializer.Deserialize<Hall>(r.Json, ShowJson.Options))
            .OfType<Hall>()
            .Select(h => new HallRow(h.Id, h.Name, h.ActiveGameId, false, MusicOnly(h)))
            .ToList();
    }
}
