using System.Text.Json;
using RedAnts.Show.Domain;
using RedAnts.Show.Features.Admin;

namespace RedAnts.Show.Features.TabBoards.Infrastructure;

public sealed class TabBoardRepository(IDJSettings settings) : ITabBoardRepository
{
    public const string Key = "Show:TabBoard";

    public async Task<TabBoard> GetAsync()
    {
        await settings.LoadAsync();
        if (settings.Get(Key) is not { Length: > 0 } json) return TabBoard.Empty;
        try { return JsonSerializer.Deserialize<TabBoard>(json, ShowJson.Options) ?? TabBoard.Empty; }
        catch (JsonException) { return TabBoard.Empty; }
    }

    public Task SaveAsync(TabBoard board) =>
        settings.SetAsync(Key, JsonSerializer.Serialize(board, ShowJson.Options));
}
